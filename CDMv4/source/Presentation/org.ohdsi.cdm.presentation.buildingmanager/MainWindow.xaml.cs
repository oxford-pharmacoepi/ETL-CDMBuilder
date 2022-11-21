﻿using System;
using System.Configuration;
using System.Windows;
using org.ohdsi.cdm.framework.shared.Helpers;

namespace org.ohdsi.cdm.presentation.buildingmanager
{
   public partial class MainWindow
   {
      private readonly BuilderViewModel vm;

      public MainWindow()
      {
         InitializeComponent();

         if (IsBuilderConnectionUnavailable())
         {
            Application.Current.Shutdown();
            return;
         }

         vm = new BuilderViewModel();
         DataContext = vm;

         var s = new Style();
         s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
         tabControl.ItemContainerStyle = s;
         tabControl.SelectedIndex = 0;
         this.Closing += MainWindow_Closing;
      }

      private static bool IsBuilderConnectionUnavailable()
      {
         if (ConfigurationManager.ConnectionStrings["Builder"] == null)
         {
            MessageBox.Show(
               "Connection to Builder database was missed in configuration file." + Environment.NewLine +
               "Please check app.config file.", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return true;
         }

         var splashScreen = new SplashScreen("Resources/splash.png");
         try
         {
            splashScreen.Show(false);
            using (
               var connection =
                  SqlConnectionHelper.OpenConnection(ConfigurationManager.ConnectionStrings["Builder"].ConnectionString)
               )
            {
            }
            splashScreen.Close(TimeSpan.FromSeconds(0));
         }
         catch (Exception)
         {
            splashScreen.Close(TimeSpan.FromSeconds(0));
            MessageBox.Show(
               "Connection to Builder database not available." + Environment.NewLine + "Please check app.config file.",
               "Configuration error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return true;
         }
         return false;
      }

      void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
      {
         vm.Stop();
      }
   }
}
