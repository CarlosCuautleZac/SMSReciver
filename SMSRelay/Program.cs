using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpAdbClient;
using SMSRelay;

var adb = new AdbClient();
var device = adb.GetDevices().FirstOrDefault();
if (device == null)
{
    Console.WriteLine("No Android device found.");
    return;
}

var reciver = new ConsoleOutputReceiver();
var command = "content query --uri content://sms/inbox --projection address,body,date";
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
            var command = "content query --uri content://sms/inbox --projection address,body,date";
            adb.ExecuteRemoteCommand(command, device, reciver);
            var messages = reciver.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); ;
            List<SMS> Nuevos = messages.Select(x => ConvertirSMS(x)).ToList();


            var recienllegados = Nuevos.Where(x => !Actuales.Any(y => y.Mensaje == x.Mensaje && y.Contacto == x.Contacto && y.Fecha == x.Fecha)).ToList();

            //recienllegados.ForEach(x => Console.WriteLine(x));
            foreach (var item in recienllegados)
            {
                Console.WriteLine(item);
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
    Dictionary<string, string> rowDict = new Dictionary<string, string>();

    // Definir la expresión regular para extraer las llaves y valores
    //string pattern = @"(\w+)=(\S+),?";
    string pattern = @"(\w+)=([\s\w:.\/\+]+),?";

    Regex regex = new Regex(pattern);

    // Obtener todas las coincidencias de la expresión regular en la entrada
    MatchCollection matches = regex.Matches(inputString);

    // Iterar sobre las coincidencias y agregarlas al diccionario
    foreach (Match match in matches)
    {
        string key = match.Groups[1].Value;
        string value = match.Groups[2].Value;
        rowDict[key] = value;
    }

    return rowDict;
}