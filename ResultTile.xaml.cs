using System;
using System.Windows;
using System.Windows.Controls;

namespace Legacinator
{
    /// <summary>
    ///     Interaction logic for ResultTile.xaml
    /// </summary>
    public partial class ResultTile : UserControl
    {
        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ResultTile),
                new PropertyMetadata(string.Empty));

        public ResultTile()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public event Action Clicked;

        private void Tile_OnClick(object sender, RoutedEventArgs e)
        {
            Clicked?.Invoke();
        }
    }
}