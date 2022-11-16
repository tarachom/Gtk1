using Gtk;
using Npgsql;

namespace GtkTest
{
    class WindowStart : Window
    {
        NpgsqlDataSource? DataSource { get; set; }

        ListStore? Store;
        TreeView? treeView;

        public WindowStart() : base("PostgreSQL + GTKSharp")
        {
            SetDefaultSize(1600, 900);
            SetPosition(WindowPosition.Center);

            DeleteEvent += delegate { Program.Quit(); };

            VBox vbox = new VBox();
            Add(vbox);

            #region Кнопки

            //Кнопки
            HBox hBoxButton = new HBox();
            vbox.PackStart(hBoxButton, false, false, 10);

            Button bConnect = new Button("Підключитись до PostgreSQL");
            bConnect.Clicked += OnConnect;
            hBoxButton.PackStart(bConnect, false, false, 10);

            Button bFill = new Button("Заповнити даними");
            bFill.Clicked += OnFill;
            hBoxButton.PackStart(bFill, false, false, 10);

            Button bAdd = new Button("Додати один запис");
            bAdd.Clicked += OnAdd;
            hBoxButton.PackStart(bAdd, false, false, 10);

            Button bSave = new Button("Зберегти зміни");
            bSave.Clicked += OnSave;
            hBoxButton.PackStart(bSave, false, false, 10);

            Button bDelete = new Button("Видалити");
            bDelete.Clicked += OnDelete;
            hBoxButton.PackStart(bDelete, false, false, 10);

            #endregion

            //Список
            HBox hboxTree = new HBox();
            vbox.PackStart(hboxTree, true, true, 0);

            AddColumn();

            hboxTree.PackStart(treeView, true, true, 10);

            ShowAll();
        }

        enum Columns
        {
            image,
            id,
            name,
            desc
        }

        void AddColumn()
        {
            Store = new ListStore
            (
                typeof(Gdk.Pixbuf),
                typeof(int),       //id
                typeof(string),    //name
                typeof(string)     //desc
            );

            treeView = new TreeView(Store);
            treeView.Selection.Mode = SelectionMode.Multiple;

            treeView.AppendColumn(new TreeViewColumn("", new CellRendererPixbuf(), "pixbuf", (int)Columns.image));
            treeView.AppendColumn(new TreeViewColumn("id", new CellRendererText(), "text", (int)Columns.id) { MinWidth = 100 });

            CellRendererText nameRendererText = new CellRendererText() { Editable = true };
            nameRendererText.Edited += OnNameEdited;

            treeView.AppendColumn(new TreeViewColumn("name", nameRendererText, "text", (int)Columns.name) { MinWidth = 500 });

            CellRendererText descRendererText = new CellRendererText() { Editable = true };
            descRendererText.Edited += OnDescEdited;

            treeView.AppendColumn(new TreeViewColumn("desc", descRendererText, "text", (int)Columns.desc) { MinWidth = 500 });
        }

        void OnNameEdited(object sender, EditedArgs args)
        {
            CellRenderer cellRender = (CellRenderer)sender;

            TreeIter iter;
            Store!.GetIterFromString(out iter, args.Path);
            Store!.SetValue(iter, (int)Columns.name, args.NewText);
        }

        void OnDescEdited(object sender, EditedArgs args)
        {
            CellRenderer cellRender = (CellRenderer)sender;

            TreeIter iter;
            Store!.GetIterFromString(out iter, args.Path);
            Store!.SetValue(iter, (int)Columns.desc, args.NewText);
        }

        void OnConnect(object? sender, EventArgs args)
        {
            string Server = "localhost";
            string UserId = "postgres";
            string Password = "1";
            int Port = 5432;
            string Database = "test";

            string conString = $"Server={Server};Username={UserId};Password={Password};Port={Port};Database={Database};SSLMode=Prefer;";

            DataSource = NpgsqlDataSource.Create(conString);

            OnFill(this, new EventArgs());
        }

        void OnFill(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                Store!.Clear();

                NpgsqlCommand command = DataSource.CreateCommand(
                    "SELECT id, name, \"desc\" FROM tab1 ORDER BY id");

                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = (int)reader["id"];
                    string name = reader["name"].ToString() ?? "";
                    string desc = reader["desc"].ToString() ?? "";

                    Store!.AppendValues(new Gdk.Pixbuf("doc.png"), id, name, desc);
                }
            }
        }

        void OnAdd(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                NpgsqlCommand command = DataSource.CreateCommand(
                    "INSERT INTO tab1 (name, \"desc\") VALUES (@name, @desc)");

                command.Parameters.Add(new NpgsqlParameter("name", "test"));
                command.Parameters.Add(new NpgsqlParameter("desc", "test"));

                command.ExecuteNonQuery();

                OnFill(this, new EventArgs());
            }
        }

        void OnSave(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                NpgsqlCommand command = DataSource.CreateCommand(
                    "UPDATE tab1 SET name = @name, \"desc\" = @desc WHERE id = @id");

                TreeIter iter;
                if (Store!.GetIterFirst(out iter))
                    do
                    {
                        int id = (int)Store!.GetValue(iter, (int)Columns.id);
                        string name = (string)Store!.GetValue(iter, (int)Columns.name);
                        string desc = (string)Store!.GetValue(iter, (int)Columns.desc);

                        command.Parameters.Clear();
                        command.Parameters.Add(new NpgsqlParameter("id", id));
                        command.Parameters.Add(new NpgsqlParameter("name", name));
                        command.Parameters.Add(new NpgsqlParameter("desc", desc));

                        command.ExecuteNonQuery();
                    }
                    while (Store.IterNext(ref iter));
            }
        }

        void OnDelete(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                if (treeView!.Selection.CountSelectedRows() != 0)
                {
                    NpgsqlCommand command = DataSource.CreateCommand(
                        "DELETE FROM tab1 WHERE id = @id");

                    TreePath[] selectionRows = treeView.Selection.GetSelectedRows();

                    foreach (TreePath itemPath in selectionRows)
                    {
                        TreeIter iter;
                        treeView.Model.GetIter(out iter, itemPath);

                        int id = (int)treeView.Model.GetValue(iter, (int)Columns.id);

                        command.Parameters.Clear();
                        command.Parameters.Add(new NpgsqlParameter("id", id));

                        command.ExecuteNonQuery();
                    }

                    OnFill(this, new EventArgs());
                }
            }
        }

    }
}
