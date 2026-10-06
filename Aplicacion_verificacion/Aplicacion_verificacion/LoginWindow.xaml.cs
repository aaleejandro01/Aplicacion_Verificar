using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Firebase.Auth;
using Firebase.Auth.Providers;

namespace Aplicacion_verificacion
{
    public partial class LoginWindow : Window
    {
        private const string FirebaseApiKey = "AIzaSyBQOHx8Pzyuj53vISwZHZ8AIrdCgfUnAyE";

        private const string FirebaseAuthDomain = "aplicacion-verificacion.firebaseapp.com";
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text;
            string password = TxtPassword.Password;

            //Comprueba que ambos campos no esten vacios
            if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TxtEstado.Foreground = System.Windows.Media.Brushes.Red;
                TxtEstado.Text = "Por favor, rellene todos los campos.";
                return;
            }

            try
            {
                TxtEstado.Foreground = System.Windows.Media.Brushes.Orange;
                TxtEstado.Text = "Conectando con el servidor...";
                BtnLogin.IsEnabled = false; //Bloqueamos el botón para que no lo pulse dos veces

                var config = new FirebaseAuthConfig
                {
                    ApiKey = FirebaseApiKey,
                    AuthDomain = FirebaseAuthDomain,
                    Providers = new FirebaseAuthProvider[]
                    {
                        new EmailProvider()
                    }
                };

                var client = new FirebaseAuthClient(config);

                var userCredential = await client.SignInWithEmailAndPasswordAsync(email, password);
                
                string uidUsuario = userCredential.User.Info.Uid; //Guardamos el uid por si nos hace falta mas tarde

                TxtEstado.Foreground = System.Windows.Media.Brushes.Green;
                TxtEstado.Text = "Acceso concedido";

                //Abrimos la aplicacion ppal
                MainWindow ventanaPrincipal = new MainWindow();
                ventanaPrincipal.Show();
                
                this.Close();
            }
            catch (FirebaseAuthException ex)
            {
                TxtEstado.Foreground = System.Windows.Media.Brushes.Red;
                if (ex.Reason == AuthErrorReason.UnknownEmailAddress || ex.Reason == AuthErrorReason.WrongPassword)
                {
                    TxtEstado.Text = "Correo o contraseña incorrectos.";
                }
                else
                {
                    TxtEstado.Text = $"Error de Firebase: {ex.Reason}";
                }

            }
            catch (Exception ex)
            {
                TxtEstado.Foreground = System.Windows.Media.Brushes.Red;
                TxtEstado.Text = $"Error Critico: {ex.Message}";
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }
    }
}
