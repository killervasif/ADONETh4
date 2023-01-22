using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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

namespace ADONETh4;

public partial class MainWindow : Window
{
    string? connectionString = null;
    DataTable? table = null;
    public MainWindow()
    {
        InitializeComponent();
        Configure();
    }

    private void Configure()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        connectionString = configuration.GetConnectionString("Library");
        table = new();

        table.Columns.Add("Id");
        table.Columns.Add("Name");
        table.Columns.Add("Pages");
        table.Columns.Add("YearPress");
        table.Columns.Add("Id_Author");
        table.Columns.Add("Id_Themes");
        table.Columns.Add("Id_Category");
        table.Columns.Add("Id_Press");
        table.Columns.Add("Comment");
        table.Columns.Add("Quantity");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();

            AsyncCallback callback = new AsyncCallback(CallBackLoaded);

            using SqlCommand command = new SqlCommand("WAITFOR DELAY '00:00:04'; SELECT * FROM Authors;", connection);
            command.BeginExecuteReader(callback, command);

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CallBackLoaded(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                var dataTable = new DataTable();

                while (reader.Read())
                {
                    int? id = reader["Id"] as int?;
                    string? firstName = reader["FirstName"] as string;
                    string? lastName = reader["LastName"] as string;

                    Dispatcher.Invoke(() => Authors.Items.Add(id + " " + firstName + " " + lastName));
                }

                MessageBox.Show("Authors Loaded");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }

    private void Authors_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!Categories.IsEnabled)
            Categories.IsEnabled = !Categories.IsEnabled;

        Categories.Items.Clear();


        try
        {
            var connection = new SqlConnection(connectionString);

            connection?.Open();
            AsyncCallback callback = new AsyncCallback(CallBackAuthors);

            var id = Authors.SelectedItem.ToString()?.Split(' ')[0];

            if (id is null)
                return;

            using SqlCommand command = new SqlCommand($"WAITFOR DELAY '00:00:02'; SELECT DISTINCT Categories.[Name] FROM Categories\r\nJOIN Books ON Id_Category = Categories.Id\r\nJOIN Authors ON Id_Author = Authors.Id\r\nWHERE Authors.Id = {id}", connection);
            command.BeginExecuteReader(callback, command);

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CallBackAuthors(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                while (reader.Read())
                    Dispatcher.Invoke(() => Categories.Items.Add(reader["Name"] as string));

                MessageBox.Show("Categories Loaded");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
            finally
            {
                reader?.Close();
            }
        }
    }

    private void Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (Categories.Items.IsEmpty)
            return;

        try
        {
            var connection = new SqlConnection(connectionString);
            connection?.Open();

            var id = Authors.SelectedItem.ToString()?.Split(' ')[0];
            var name = Categories.SelectedItem.ToString();

            using SqlCommand command = new SqlCommand($"WAITFOR DELAY '00:00:02'; SELECT Books.Id, Books.Name, Pages, YearPress, Id_Themes, Id_Category, Id_Author, Id_Press, Comment, Quantity FROM Books\r\nJOIN Categories ON Categories.Id = Id_Category \r\nJOIN Authors ON Authors.Id = Id_Author \r\nWHERE Categories.Name = '{name}' AND Id_Author = {id}\r\n", connection);
            AsyncCallback callback = new AsyncCallback(CallBackCategories);

            command.BeginExecuteReader(callback, command);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CallBackCategories(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                while (reader.Read())
                {
                    var row = table?.NewRow();

                    if (row != null)
                    {
                        row["Id"] = reader["Id"];
                        row["Name"] = reader["Name"];
                        row["Pages"] = reader["Pages"];
                        row["YearPress"] = reader["YearPress"];
                        row["Id_Author"] = reader["Id_Author"];
                        row["Id_Themes"] = reader["Id_Themes"];
                        row["Id_Category"] = reader["Id_Category"];
                        row["Id_Press"] = reader["Id_Press"];
                        row["Comment"] = reader["Comment"];
                        row["Quantity"] = reader["Quantity"];

                        table?.Rows.Add(row);
                    }
                }

                Dispatcher.Invoke(() => Books.ItemsSource = table?.AsDataView());
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchTxt.Text))
            return;

        table?.Rows.Clear();

        try
        {
            var connection = new SqlConnection(connectionString);
            connection?.Open();

            using SqlCommand command = new SqlCommand($"WAITFOR DELAY '00:00:02'; SELECT * FROM Books\r\nWHERE Name LIKE '%{SearchTxt.Text}%'", connection);
            AsyncCallback callback = new AsyncCallback(CallBackSearch);

            command.BeginExecuteReader(callback, command);


        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void CallBackSearch(IAsyncResult result)
    {
        if (result.AsyncState is SqlCommand command)
        {
            SqlDataReader? reader = null;

            try
            {
                reader = command.EndExecuteReader(result);

                while (reader.Read())
                {
                    var row = table?.NewRow();

                    if (row != null)
                    {
                        row["Id"] = reader["Id"];
                        row["Name"] = reader["Name"];
                        row["Pages"] = reader["Pages"];
                        row["YearPress"] = reader["YearPress"];
                        row["Id_Author"] = reader["Id_Author"];
                        row["Id_Themes"] = reader["Id_Themes"];
                        row["Id_Category"] = reader["Id_Category"];
                        row["Id_Press"] = reader["Id_Press"];
                        row["Comment"] = reader["Comment"];
                        row["Quantity"] = reader["Quantity"];

                        table?.Rows.Add(row);
                    }

                }
                Dispatcher.Invoke(() => Books.ItemsSource = table?.AsDataView());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
    }
}
