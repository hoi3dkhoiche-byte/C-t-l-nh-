using System;
using System.Windows.Forms;
using CoTuLenh.Client.Forms;

namespace CoTuLenh.Client
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
