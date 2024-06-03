using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ProjectFolderApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    class Utils
    {
        public static IList<Group> GetGroups()
        {
            List<Group> groups = new List<Group>();
            SearchResultCollection results;
            DirectorySearcher ds = null;
            DirectoryEntry de = new DirectoryEntry(GetDomainPath());
            ds = new DirectorySearcher(de);
            // Sort by name
            ds.Sort = new SortOption("name", SortDirection.Ascending);
            ds.PropertiesToLoad.Add("name");
            ds.PropertiesToLoad.Add("memberof");
            ds.PropertiesToLoad.Add("member");
            // Search Query to find only Group objects that start with
            ds.Filter = "(&(objectCategory=Group)(cn=)(!(cn=))(!(cn=)))";
            results = ds.FindAll();

            foreach (SearchResult sr in results)
            {
                if (sr.Properties["name"].Count > 0)
                    //Debug.WriteLine(sr.Properties["name"][0].ToString());
                    groups.Add(new Group() { Name = sr.Properties["name"][0].ToString() });
            }
            return groups;
        }
        public static string GetDomainPath()
        {
            DirectoryEntry de = new DirectoryEntry("LDAP://RootDSE");
            return "LDAP://" + de.Properties["defaultNamingContext"][0].ToString();
        }
        public static List<IdentityReference> GetDirectorySecurity(string folderName)
        {
            var globals = new GlobalVars();
            string root = globals.getRootDir();

            String newPath = System.IO.Path.Combine(root, folderName);

            DirectorySecurity directorySecurity = new DirectorySecurity(newPath, AccessControlSections.Access);
            // Get the list of access rules
            AuthorizationRuleCollection accessRules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));

            // Extract the IdentityReference from each access rule
            List<IdentityReference> accessListNames = new List<IdentityReference>();
            foreach (AuthorizationRule rule in accessRules)
            {
                if (rule is FileSystemAccessRule fileSystemAccessRule)
                {
                    accessListNames.Add(fileSystemAccessRule.IdentityReference);
                }
            }
            return accessListNames;
        }
    }
    public class GlobalVars
    {
        //public static string rootdir = @"C:\Projects\";
        public static string rootdir = @"\\IPADDRESS\Server\Share\";
        public static string adminDir = @"Admin";
        public static string projMgmtDir = @"Proj Mgmt";
        public static string techDir = @"Tech";
        //--------------------------------------------
        //Sub-Directory Paths
        //--------------------------------------------
        public static string[] subDir = { "Admin", "Proj Mgmt", "Tech" };
        public static string[] subDirAdmin = { "Closeout", "Contacts", "Corresp", "Mtg Min", "Permits-Approvals", "Reports", "Reviews", "Schedule", "Submissions" };
        public static string[] subDirProjMgmt = { "Budget", "Contract", "Invoices", "Proposals", "Subc Agreements" };

        public string getRootDir()
        {
            return rootdir;
        }
    }

    public partial class MainWindow : Window
    {
        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            getProjects();
        }
        private void getProjects()
        {
            var globals = new GlobalVars();
            DirectoryInfo dir = new DirectoryInfo(globals.getRootDir());
            List<Folder> folders = new List<Folder>();
            foreach (var item in dir.GetDirectories())
            {
                folders.Add(new Folder() { Name = item.Name, CreateTime = item.CreationTime });
            }
            lvFolders.ItemsSource = folders;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvFolders.ItemsSource);
            view.Filter = FolderFilter;
        }
        private bool FolderFilter(object item)
        {
            if (String.IsNullOrEmpty(txtFilter.Text))
                return true;
            else
                return (item as Folder).Name.Contains(txtFilter.Text, StringComparison.OrdinalIgnoreCase);
        }
        private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvFolders.ItemsSource).Refresh();
        }
        private void CreateFolderBtn_Click(object sender, System.EventArgs e)
        {
            Window projWin = new CreateProjWindow();
            projWin.ShowDialog();
            if (projWin.DialogResult == true)
            {
                // Reload list of folders
                getProjects();
            }
        }
        private void ListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem Item = (ListViewItem)sender;
            Folder SelectedFolder = (Folder)Item.Content;
            Window updateProjWin = new UpdateProjWindow
            {
                SelectedProject = SelectedFolder.Name
            };
            updateProjWin.ShowDialog();
        }
        private void lvFoldersColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                lvFolders.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            if (sortBy == "Time")
            {
                listViewSortCol = column;
                listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
                AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                lvFolders.Items.SortDescriptions.Add(new SortDescription("CreateTime", newDir));
            }
            if (sortBy == "Name")
            {
                listViewSortCol = column;
                listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
                AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                lvFolders.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            }

        }
        public class SortAdorner : Adorner
        {
            private static Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            public ListSortDirection Direction { get; private set; }

            public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
            {
                this.Direction = dir;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                TranslateTransform transform = new TranslateTransform
                    (
                        AdornedElement.RenderSize.Width - 15,
                        (AdornedElement.RenderSize.Height - 5) / 2
                    );
                drawingContext.PushTransform(transform);

                Geometry geometry = ascGeometry;
                if (this.Direction == ListSortDirection.Descending)
                    geometry = descGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }
    }

    public class Folder
    {
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
    }
    public class Group
    {
        public bool isChecked { get; set; }
        public string Name { get; set; }
    }


}