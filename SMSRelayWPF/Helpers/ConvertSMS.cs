using SMSRelayWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SMSRelayWPF.Helpers
{
    public class ConvertSMS
    {
        public SMS ConvertirSMS(string inputString)
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

        Dictionary<string, string> ParseRow(string inputString)
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
    }


    
}
