using System.Collections.ObjectModel;

namespace DesktopDocumentSigner
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        public ObservableCollection<string> LogEntries { get; set; }

        public MainPage()
        {
            InitializeComponent();
            LogEntries = new ObservableCollection<string>
            {
                "App started",
                "No documents detected",
                "Ready for new document"
            };
            BindingContext = this;
        }
    }

}
