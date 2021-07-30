﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ConceptPad.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ConceptPad.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Concept> concepts;
        public MainPage()
        {
            concepts = new ObservableCollection<Concept>();
            concepts.Add(new Concept { Name = "Name", Type ="Game", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name2", Type ="Game2", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            concepts.Add(new Concept { Name = "Name3", Type ="Game3", Description = "Description", DateCreated = DateTime.Now, IsInProduction = false});
            this.InitializeComponent();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
