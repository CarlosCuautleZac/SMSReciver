using SharpAdbClient;
using SMSRelayWPF.Helpers;
using SMSRelayWPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SMSRelayWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {


        public List<SMS>? Actuales { get; set; } = new();
        public List<SMS>? Nuevos { get; set; } = new();
        public SMS SMSActual { get; set; } = new();
        public SMS SMSAnterior { get; set; } = new();

        public ObservableCollection<SMS>? SMSPantalla { get; set; } = new();

        string? command { get; set; }
        bool conectado = false;
        //long unixtime;

        #region Objects
        AdbClient adb;
        SerialPort puertoSerie;
        ConvertSMS convertSMS;
        DeviceData? device;
        Dispatcher dispatcher;



        #endregion
        public MainViewModel()
        {
            adb = new AdbClient();
            puertoSerie = new SerialPort("COM3", 9600);
            convertSMS = new();
            dispatcher = Dispatcher.CurrentDispatcher;
            Iniciar();
        }

        private void Iniciar()
        {
            bool result = false;

            Task task = new Task(() =>
            {
                result = Conect();
                Actualizar();
            });

            task.Start();
            task.Wait();

            if (result)
            {
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {

                            var reciver = new ConsoleOutputReceiver();
                            adb.ExecuteRemoteCommand(command, device, reciver);
                            var messages = reciver.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); ;
                            Nuevos = messages.Select(x => convertSMS.ConvertirSMS(x)).ToList();



                            var recienllegados = Nuevos.Where(x => !Actuales.Any(y => y.Mensaje == x.Mensaje && y.Contacto == x.Contacto && y.Fecha == x.Fecha)).ToList();
                            //if(recienllegados.Count > 300) 
                            //{
                            //    recienllegados.Clear();
                            //}

                            //recienllegados.ForEach(x => Console.WriteLine(x));
                            foreach (var item in recienllegados)
                            {
                                dispatcher.Invoke(() =>
                                {
                                    if (SMSPantalla != null && SMSActual.Contacto != "")
                                        SMSPantalla.Add(SMSActual);

                                    SMSActual = item;

                                    if (SMSPantalla != null)
                                        SMSPantalla = new(SMSPantalla.OrderByDescending(x => x.Fecha).ToList());

                                    Actualizar();
                                    Task.Delay(1000);
                                });

                                puertoSerie.Write(item.Contacto + "," + item.Mensaje);


                            }

                            dispatcher.Invoke((Action)(() =>
                            {
                                if (Actuales != null)
                                    Actuales.AddRange(recienllegados);

                                Actualizar();
                            }));





                            Thread.Sleep(2000);
                        }
                        catch (Exception m)
                        {
                            Console.WriteLine(m.Message);
                        }
                    }
                });

                t.IsBackground = true;
                t.Start();
            }
        }

        private bool Conect()
        {
            try
            {
                puertoSerie.Open();
                conectado = true;
                if (conectado)
                {
                    DateTime currentTime = DateTime.Now;
                    long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
                    command = $"content query --uri content://sms/inbox --projection address,date,body --where \"date>={unixTime}\"";
                    device = adb.GetDevices().FirstOrDefault();

                    if (device == null)
                    {
                        throw new ApplicationException("No Android device found.");

                    }
                    var reciver = new ConsoleOutputReceiver();
                    adb.ExecuteRemoteCommand(command, device, reciver);

                    var messages = reciver.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); ;

                    Actuales = messages.Select(x => convertSMS.ConvertirSMS(x)).OrderByDescending(x => x.Fecha).ToList();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                conectado = false;
                puertoSerie.Close();
                System.Windows.Application.Current.Shutdown();
                return false;
            }
        }

        public void Actualizar(string nombre = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
