using System;
using System.Windows.Controls;

using MahApps.Metro.IconPacks;

namespace Legacinator;

public partial class CustomResultTile : UserControl
{
    public CustomResultTile(string title, Action onClicked, PackIconForkAwesomeKind kind)
    {
        InitializeComponent();

        MainTile.Title = title;
        MainTile.Click += delegate
        {
            onClicked.Invoke();
        };
        MainIcon.Kind = kind;
    }
}