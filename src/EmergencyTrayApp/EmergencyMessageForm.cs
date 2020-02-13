using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmergencyTrayApp
{
    public class EmergencyMessageItem : TableEntity
    {
        public EmergencyMessageItem()
        {
        }
        public EmergencyMessageItem(string messageId, string userId)
        {
            PartitionKey = messageId;
            RowKey = userId;
        }
        public int ExpireInMinutes { get; set; }
        public string Message { get; set; }
        public int Active { get; set; }
    }
    public partial class EmergencyMessageForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        public EmergencyMessageForm()
        {
            InitializeComponent();
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();           
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Petrofac Emergency Agent";
            trayIcon.Icon = new Icon(SystemIcons.Information, 40, 40);
            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        private void EmergencyMessageForm_Load(object sender, EventArgs e)
        {
            CloudStorageAccount backupStorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var backupBlobClient = backupStorageAccount.CreateCloudTableClient();
            var containerClient = backupBlobClient.GetTableReference("EmergencyMessages");
            while (true)
            {
                CheckForMessage(containerClient);
                Thread.Sleep(3000);
            }
            //EventHubCheck();
        }
        private static async Task CheckForMessage(CloudTable tableClient)
        {
            TableQuery<EmergencyMessageItem> query = new TableQuery<EmergencyMessageItem>();
            DateTimeOffset expiryTime;

            foreach (EmergencyMessageItem entity in tableClient.ExecuteQuery(query))
            {
                expiryTime = entity.Timestamp.AddMinutes(entity.ExpireInMinutes);
                if (entity.Active ==1)
                {
                    if (expiryTime > DateTimeOffset.Now)
                    {
                        string alertMessage = String.Format("{0} \n Expires in: {1}", entity.Message, expiryTime.ToString());
                        MessageBox.Show(new Form { TopMost = true }, alertMessage, "Emergency!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }               
            }
        }

      

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; 
            ShowInTaskbar = false; 
            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
