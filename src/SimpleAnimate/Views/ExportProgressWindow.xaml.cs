using System.Windows;

namespace SimpleAnimate.Views;

public partial class ExportProgressWindow : Window
{
    public ExportProgressWindow()
    {
        InitializeComponent();
    }

    public void UpdateProgress(double percent, string status)
    {
        ExportProgress.Value = percent;
        PercentText.Text = $"{(int)percent}%";
        StatusText.Text = status;
    }
}
