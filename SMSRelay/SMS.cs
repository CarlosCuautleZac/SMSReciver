using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSRelay
{
    internal class SMS
    {
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Contacto { get; set; } = "";

        public override string ToString()
        {
            return $"Contacto:{Contacto}\n {Mensaje}";
        }
    }
}
