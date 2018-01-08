using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LISA.Dblib;

namespace LISA.Backend
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {

        #region Fields

        private static LISAEntities _Entities;

        #endregion

        #region Properties

        public static LISAEntities Entities => _Entities;

        #endregion

        #region Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _Entities = new LISAEntities();
                _Entities.Database.Connection.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur", "Impossible de joindre la base de données" + Environment.NewLine +
                    ex.ToString());
                this.Shutdown();
            }
        }

        #endregion
    }
}
