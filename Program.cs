
//=============================================================//
//
//                      Project Info
//
//=============================================================//
//
//                          ANPR
//
//=============================================================//
//
//                      Group Members
//      1. Muhammad Ibrahim         120665
//      2. Muhammad Arham Shahzad   120623
//      3. ZaheerUdin Babar         120645 
//
//=============================================================//
//
//
//      .Help Taken by Irtiza Hasan in understanding of Mser (Thersholding)
//
//
//=============================================================//
//
//                  Libraies Used
//      1.Aforge (Latest Version) => for filters
//      2.Emguc  (latest Version) => for OCr 
//
//=============================================================//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace anprTry1
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
