using LISA.Dblib;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LISA.Backend.Views
{
    /// <summary>
    /// Logique d'interaction pour MagasinView.xaml
    /// </summary>
    public partial class MagasinView : UserControl
    {
        public MagasinView()
        {
            InitializeComponent();
        }

        #region Data

        private void RefreshData()
        {
            //Pour les éléments présents dans le cache local
            foreach (Magasin magasin in App.Entities.Magasins.Local)
            {
                //On récupère le DbEntityEntryassocié
                DbEntityEntry<Magasin> entry = App.Entities.Entry<Magasin>(magasin);

                //Si il n'a pas été modifié, ajouter ou suppirmer (Unchanged)
                if (entry.State == System.Data.Entity.EntityState.Unchanged)
                {
                    //On recharge l'élément
                    entry.Reload();
                }
            }

            //Charge dans EntityFramework les magasins qui ne sont pas déjà chargés
            App.Entities.Magasins.ToList();

            _ListBoxShops.ItemsSource = null;
            _ListBoxShops.ItemsSource = App.Entities.Magasins.Local;
        }

        #endregion
    }
}
