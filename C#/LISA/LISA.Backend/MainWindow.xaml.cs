using System;
using System.Collections.Generic;
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
using LISA.Dblib;

namespace LISA.Backend
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Exemple d'abonnement à un évènement en C#
            //_MenuItemExit.Click += _MenuItemExit_Click;

            //Charge dans Entity Framework les magasins
            App.Entities.Magasins.ToList();

            //Affiche les données chargés dans EF(vue local)
            _ListBoxShops.ItemsSource = App.Entities.Magasins.Local;
        }

        #region Methods

        #region Menu

        #region File

        private void _MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void _MenuItemSave_Click(object sender, RoutedEventArgs e)
        {

            if (App.Entities.ChangeTracker.HasChanges())
            {
                App.Entities.SaveChanges();
            }

        }
        private void _MenuItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            App.Entities.Magasins.ToList();
        }



        #endregion

        #region Edit

        private void _MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            Magasin mag = new Magasin() { Libelle = "Nouveau" };
            App.Entities.Magasins.Add(mag);
            _ListBoxShops.SelectedItem = mag;
        }

        private void _MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            Magasin mag = _ListBoxShops.SelectedItem as Magasin;
            if (mag != null)
            {
                App.Entities.Magasins.Remove(mag);
            }
        }

        #endregion

        #endregion

        #endregion

    }
}
