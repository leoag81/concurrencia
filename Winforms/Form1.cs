using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Winforms
{
    public partial class Form1 : Form
    {
        private string apiURL;
        private HttpClient httpClient;
        public Form1()
        {
            InitializeComponent();
            apiURL = "https://localhost:44397";
            httpClient = new HttpClient();
        }

        private async void botonIniciar_Click(object sender, EventArgs e)
        {
            loadingGIF.Visible = true;
            var tarjetas = await ObtenerTarjetasCredito(28000);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                await ProcesarTarjetas(tarjetas);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
            MessageBox.Show($"Operación finalizada en {stopwatch.ElapsedMilliseconds / 1000.0} segunos");
            loadingGIF.Visible = false;
        }

        private async Task ProcesarTarjetas(List<string> tarjetas) {

            using var semaforo = new SemaphoreSlim(4000);

            var tareas = new List<Task<HttpResponseMessage>>();

            tareas = tarjetas.Select(async tarjeta =>
            {
                var json = JsonConvert.SerializeObject(tarjeta);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await semaforo.WaitAsync();
                try
                {
                    return await httpClient.PostAsync($"{apiURL}/tarjetas", content);
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                    return null;
                }
                finally
                {
                    semaforo.Release();
                }
            }).ToList();

            await Task.WhenAll(tareas);
        }

        private async Task<List<string>> ObtenerTarjetasCredito(int cantidadDeTarjetas)
        {

            return await Task.Run(() =>
            {
                var tarjetas = new List<string>();
                for (int i = 0; i < cantidadDeTarjetas; i++)
                {
                    // 00000000000000001
                    // 00000000000000002
                    // etc...
                    tarjetas.Add(i.ToString().PadLeft(16, '0'));
                }
                return tarjetas;
            });

        }

        private async Task Esperar() {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        private async Task<string> ObtenerSaludo(string nombre) {
            using (var respuesta = await httpClient.GetAsync($"{apiURL}/saludos2/{nombre}"))
            {
                Console.WriteLine(respuesta);
                respuesta.EnsureSuccessStatusCode();
                var saludo = await respuesta.Content.ReadAsStringAsync();
                Console.WriteLine(saludo);
                return saludo;
            }
        }
    }
}
