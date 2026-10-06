using Aplicacion_firmado;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aplicacion_verificacion
{
    public partial class MainWindow : Window
    {
        //Variable global : Guardamos la ruta de la carpeta a la que apunta el explorador de archivos
        private string rutaCarpetaActual = "";

        //Variable global: Guardamos la ruta del archivo que vamos a verificar.
        private string rutaArchivoActual = "";

        //Variable goblar: Contiene la version de la aplicacion
        private string versionAplicacion = "v1.0";

        public MainWindow()
        {
            InitializeComponent();

            TxtVersionApp.Text = $"{versionAplicacion}";
        }

        // EVENTO: Cuando el usuario hace clic en "Abrir Carpeta..."
        private void BtnAbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            // Usamos el diálogo clásico para elegir carpeta
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Selecciona la carpeta raíz para el proyecto de firmado";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string rutaSeleccionada = dialog.SelectedPath;
                rutaCarpetaActual = rutaSeleccionada;
                CargarArbolDeArchivos(rutaSeleccionada);
            }
        }

        // Carga la carpeta en el TreeView
        private void CargarArbolDeArchivos(string rutaRaiz)
        {
            TvExplorador.Items.Clear(); // Limpiamos lo que hubiera antes

            DirectoryInfo dirInfo = new DirectoryInfo(rutaRaiz);
            TreeViewItem nodoRaiz = CrearNodoRecursivo(dirInfo);

            nodoRaiz.IsExpanded = true; // Que la carpeta principal salga abierta
            TvExplorador.Items.Add(nodoRaiz);
        }

        // Lee carpetas y archivos y los convierte en elementos visuales
        private TreeViewItem CrearNodoRecursivo(DirectoryInfo directorio)
        {
            TreeViewItem nodo = new TreeViewItem
            {
                Header = directorio.Name,
                Tag = directorio.FullName // Guardamos la ruta oculta en la propiedad Tag
            };

            try
            {
                // Primero añadimos las subcarpetas
                foreach (DirectoryInfo subDir in directorio.GetDirectories())
                {
                    nodo.Items.Add(CrearNodoRecursivo(subDir));
                }

                // Luego añadimos los archivos
                foreach (FileInfo archivo in directorio.GetFiles())
                {
                    TreeViewItem nodoArchivo = new TreeViewItem
                    {
                        Header = archivo.Name,
                        Tag = archivo.FullName // Guardamos la ruta exacta del archivo
                    };
                    nodo.Items.Add(nodoArchivo);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignoramos carpetas del sistema a las que Windows no nos deja entrar
            }

            return nodo;
        }

        // EVENTO: Cuando el usuario hace clic en un archivo del explorador
        private void TvExplorador_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TvExplorador.SelectedItem is TreeViewItem nodoSeleccionado)
            {
                if (nodoSeleccionado.Tag is string ruta)
                {
                    if (System.IO.File.Exists(ruta))
                    {
                        // 1. Guardamos la ruta completa en nuestra variable para usarla luego al firmar
                        rutaArchivoActual = ruta;

                        // 2. Cambiamos el texto del panel derecho
                        TxtNombreArchivoVerificar.Text = System.IO.Path.GetFileName(ruta);

                        // 3. Lo ponemos en color negro para que destaque (ya no es el texto gris por defecto)
                        TxtNombreArchivoVerificar.Foreground = System.Windows.Media.Brushes.Black;

                        //4. Muestra la ruta completa al pasar el ratón
                        TxtNombreArchivoVerificar.ToolTip = ruta;

                        try
                        {
                            var archivo = new ArchivoSSP(ruta);
                            if (archivo.TieneFirma)
                            {
                                // Buscamos y seleccionamos el algoritmo en el ComboBox
                                foreach (ComboBoxItem item in CmbAlgoritmoVerificar.Items)
                                {
                                    if (item.Content.ToString() == archivo.AlgoritmoDetectado)
                                    {
                                        CmbAlgoritmoVerificar.SelectedItem = item;
                                        break;
                                    }
                                }

                                // Buscamos y seleccionamos el Hash
                                foreach (ComboBoxItem item in CmbHashVerificar.Items)
                                {
                                    if (item.Content.ToString() == archivo.HashDetectado)
                                    {
                                        CmbHashVerificar.SelectedItem = item;
                                        break;
                                    }
                                }

                                TxtMensajeEstadoVerificar.Text = "Configuración de firma detectada automáticamente.";
                                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Blue;
                            }
                        }
                        catch { /* Ignorar si falla la lectura rápida */ }
                    }
                }
            }
        }

        // EVENTO VISUAL: Ocultar el Hash en Verificar si se exige Ed25519
        private void CmbAlgoritmoVerificar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Evitamos errores si la ventana aún se está dibujando
            if (CmbAlgoritmoVerificar == null || CmbHashVerificar == null) return;

            if (CmbAlgoritmoVerificar.SelectedItem is ComboBoxItem itemSeleccionado)
            {
                string algoritmoElegido = itemSeleccionado.Content.ToString();

                if (algoritmoElegido == "EdDSA Ed25519")
                {
                    // Si es Ed25519, quitamos la selección y bloqueamos el desplegable de Hash
                    CmbHashVerificar.SelectedIndex = -1;
                    CmbHashVerificar.IsEnabled = false;
                }
                else
                {
                    // Si es cualquier ECDSA, lo volvemos a habilitar
                    CmbHashVerificar.IsEnabled = true;
                }
            }
        }

        // EVENTO: Cuando el usuario pulsa VERIFICAR FIRMA
        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            // Comprobamos que se ha seleccionado un archivo
            if (string.IsNullOrEmpty(rutaArchivoActual) || !System.IO.File.Exists(rutaArchivoActual))
            {
                TxtMensajeEstadoVerificar.Text = "❌ Selecciona un archivo original a la izquierda.";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            // Comprobamos que haya elegido un algoritmo de firmado
            if (CmbAlgoritmoVerificar.SelectedItem == null)
            {
                TxtMensajeEstadoVerificar.Text = "❌ Define el algoritmo exigido.";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            string algoritmoExigido = ((ComboBoxItem)CmbAlgoritmoVerificar.SelectedItem).Content.ToString();
            string hashExigido = "No aplica (Ed25519)";

            if (algoritmoExigido != "EdDSA Ed25519")
            {
                if (CmbHashVerificar.SelectedItem == null)
                {
                    TxtMensajeEstadoVerificar.Text = "❌ Define el Hash exigido.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                hashExigido = ((ComboBoxItem)CmbHashVerificar.SelectedItem).Content.ToString();
            }

            try
            {
                TxtMensajeEstadoVerificar.Text = "⏳ Leyendo archivo y comprobando firma incrustada...";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Orange;

                // Usamos clase ArchivoSSP para "despiezar" el archivo seleccionado.
                var archivoSsp = new ArchivoSSP(rutaArchivoActual);

                // Comprobamos si el archivo tiene el formato correcto (marcador "SG ")
                if (!archivoSsp.TieneFirma)
                {
                    TxtMensajeEstadoVerificar.Text = "❌ El archivo no contiene ninguna firma incrustada (falta 'SG ').";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Extraemos la firma y el contenido limpio directamente de la clase.
                string firmaBase64 = archivoSsp.FirmaExtraida;
                byte[] firmaBytes = Convert.FromBase64String(firmaBase64);
                byte[] datosOriginales = archivoSsp.ContenidoOriginal; // Este es el mensaje SIN la última línea.

                string carpetaClaves = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Claves");
                string rutaClavePem = System.IO.Path.Combine(carpetaClaves, $"{algoritmoExigido}.pem");

                if (!System.IO.File.Exists(rutaClavePem))
                {
                    TxtMensajeEstadoVerificar.Text = $"❌ No se encuentra la clave para {algoritmoExigido}.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                string clavePem = System.IO.File.ReadAllText(rutaClavePem);

                using var verificador = new FirmwareVerifier(clavePem, algoritmoExigido, hashExigido);
                bool esValida = verificador.VerificarFirma(datosOriginales, firmaBytes);

                if (esValida)
                {
                    TxtMensajeEstadoVerificar.Text = "✅ FIRMA VÁLIDA. El archivo es auténtico.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TxtMensajeEstadoVerificar.Text = "🚨 FIRMA INVÁLIDA. Posible modificación.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                }
                //Guardamos la informacion de la verificacion en los logs
                string carpetaLogs = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                System.IO.Directory.CreateDirectory(carpetaLogs); // Nos aseguramos de que la carpeta exista

                string nombreArchivoLog = $"historial_{DateTime.Now:yyyy-MM-dd}.log";
                string rutaArchivoLog = System.IO.Path.Combine(carpetaLogs, nombreArchivoLog);

                string estadoLog = esValida ? "ÉXITO (Válida)" : "ALERTA (Inválida o Modificada)";
                string lineaLog = $"[{DateTime.Now:HH:mm:ss}] [VERIFICACIÓN] Archivo: {System.IO.Path.GetFileName(rutaArchivoActual)} | Algoritmo Exigido: {algoritmoExigido} / {hashExigido} | Resultado: {estadoLog}\n";

                System.IO.File.AppendAllText(rutaArchivoLog, lineaLog);
            }
            catch (Exception ex)
            {
                TxtMensajeEstadoVerificar.Text = $"❌ Error crítico: {ex.Message}";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}