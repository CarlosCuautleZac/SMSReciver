using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;
using SharpAdbClient;
using SMSRelay;
using Solid.Arduino;
using Solid.Arduino.Firmata;

var adb = new AdbClient();
SerialPort puertoSerie = new SerialPort("COM3", 9600);
try
{
    puertoSerie.Open();

}
catch(FileNotFoundException m)
{
    Console.WriteLine("No se encontro el arduino");
    Environment.Exit(0);
}

DateTime currentTime = DateTime.Now;
long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

string command = $"content query --uri content://sms/inbox --projection address,date,body --where \"date>={unixTime}\"";

var device = adb.GetDevices().FirstOrDefault();
if (device == null)
{
    Console.WriteLine("No Android device found.");
    return;
}

var reciver = new ConsoleOutputReceiver();
adb.ExecuteRemoteCommand(command, device, reciver);


//    var receiver = new ConsoleOutputReceiver();
//    var command = "content query --uri content://sms/";
//    await adb.ExecuteRemoteCommandAsync(command, device, receiver);
var messages = reciver.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); ;


List<SMS> Actuales = messages.Select(x => ConvertirSMS(x)).OrderByDescending(x=>x.Fecha).ToList();


//lanzar un hilo que se ejecute cada 10 segundos
//comparar los que ya tengo con el entrante y de ahi sacamos el nuevo

Thread t = new Thread(() =>
{
    while (true)
    {
        try
        {

            var reciver = new ConsoleOutputReceiver();
            adb.ExecuteRemoteCommand(command, device, reciver);
            var messages = reciver.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); ;
            List<SMS> Nuevos = messages.Select(x => ConvertirSMS(x)).ToList();



            var recienllegados = Nuevos.Where(x => !Actuales.Any(y => y.Mensaje == x.Mensaje && y.Contacto == x.Contacto && y.Fecha == x.Fecha)).ToList();

            //recienllegados.ForEach(x => Console.WriteLine(x));
            foreach (var item in recienllegados)
            {
                Console.WriteLine(item);

                puertoSerie.Write(item.Contacto+","+item.Mensaje);


            }


            Actuales.AddRange(recienllegados);

            Thread.Sleep(10000);
        }
        catch(Exception m)
        {
            Console.WriteLine(m.Message);
        }
    }
});

t.IsBackground = true;
t.Start();

Console.ReadLine();
puertoSerie.Close();


static SMS ConvertirSMS(string inputString)
{
    SMS sms = new SMS();

    var x = ParseRow(inputString);

    if (x.ContainsKey("body"))
        sms.Mensaje = x["body"];

    if (x.ContainsKey("address"))
        sms.Contacto = x["address"];

    if (x.ContainsKey("date"))
    {
        long unixDate = long.Parse(x["date"]);
        DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime date = start.AddMilliseconds(unixDate).ToLocalTime();
        sms.Fecha = date;
    }
    return sms;
}

static Dictionary<string, string> ParseRow(string inputString)
{

    Encoding iso = Encoding.GetEncoding("ISO-8859-1");

    // Decodificar la cadena y convertirla a UTF-8
    byte[] isoBytes = iso.GetBytes(inputString);
    string utf8String = Encoding.UTF8.GetString(isoBytes);


    Dictionary<string, string> rowDict = new Dictionary<string, string>();

    // Definir la expresión regular para extraer las llaves y valores
    //string pattern = @"(\w+)=(\S+),?";
    string pattern = @"(\w+)=([\s\w:.\/\+\?¿:#áéíóúÁÉÍÓÚñÑ]+),?";

    Regex regex = new Regex(pattern);

    // Obtener todas las coincidencias de la expresión regular en la entrada
    MatchCollection matches = regex.Matches(utf8String);

    // Iterar sobre las coincidencias y agregarlas al diccionario
    foreach (Match match in matches)
    {
        string key = match.Groups[1].Value;
        string value = match.Groups[2].Value;
        rowDict[key] = value;
    }

    return rowDict;
}