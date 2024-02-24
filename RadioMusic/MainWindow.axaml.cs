using System;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Npgsql;
using Avalonia.Input;
using Avalonia.Interactivity;
using TagLib;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RadioMusic;

public partial class MainWindow : Window
{
    private Button selectFileButton;
    private NpgsqlConnection _connection; //создание PostgreSQL подключения 
    private List<MusicClass> mus; //объявление списка mus, которое будет использовано для обращения к базе 
    string connectionString = "Server=localhost;Port=5432;Database=RadioMusic;User Id=postgres;Password=123456;"; //строка подключения к базе 
    private string fullTable = "SELECT * FROM Музыка ORDER BY Название"; //запрос отображения записей

    public MainWindow()
    {
        InitializeComponent();
        selectFileButton = this.Get<Button>("SelectFileButton"); //получение кнопки
        _connection = new NpgsqlConnection(connectionString); //создание нового подключения по строке 
        ShowTable(fullTable); //метод отображения таблицы по запросу 
        FillStatus();
    }
    
    // Отображение таблицы 
    public void ShowTable(string sql)
    {
        mus = new List<MusicClass>();
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        NpgsqlCommand command = new NpgsqlCommand(sql, _connection); //создание команды для выполнения запроса
        NpgsqlDataReader reader = command.ExecuteReader(); //выполнение запроса и получение результата
        while (reader.Read() && reader.HasRows) //чтение результатов запроса
        {
            var Music = new MusicClass()
            { 
                //получение результатов столбцов по классу 
                Name = reader.GetString(0),
                Artist = reader.GetString(1),
                Album = reader.GetString(2),
                Genre = reader.GetString(3),
                Path = reader.GetString(4)
            };
            mus.Add(Music); //добавление объекта MusicClass в список mus.
        }
        _connection.Close();
        DataGrid.ItemsSource = mus; //установка списка mus в качестве источника данных для DataGrid
        DataGrid.LoadingRow += DataGrid_LoadingRow; //событие загрузки строк 
    }
    
    // Удаление данных
    private void DeleteData_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            MusicClass msc = DataGrid.SelectedItem as MusicClass; //получение выбранного объекта MusicClass
            if (msc == null) //если строки не выделены
            {
                return; //выход из метода
            }

            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            // Использование запроса с парамтерами для предотвращения проблем с одинарными кавычками в названиях
            string sql = "DELETE FROM Музыка WHERE Название = @name";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("name", msc.Name);

            cmd.ExecuteNonQuery(); //выполнение запроса
            _connection.Close();

            mus.Remove(msc); //удаление объекта из списка 
            ShowTable(fullTable); //отображение обновленной таблицы
        }
        catch (Exception ex) //действия при возникновении ошибок
        {
            Console.WriteLine(ex.Message);
        }
    }

    // Выбор файла для внесения данных
    private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OpenFileDialog fileDialog = new OpenFileDialog(); //создание диалогового окна выбора файла
            fileDialog.Filters.Add(new FileDialogFilter() { Name = "Audio Files", Extensions = { "mp3", "ogg", "wav" } }); //ограничение на выбор только аудиофайлов

            string[]? fileNames = await fileDialog.ShowAsync(this); //отображение диалогового окна и получение выбранных файлов
            if (fileNames != null && fileNames.Length > 0) //если файл выбран
            {
                string filePath = fileNames[0]; //получение пути к выбранному файлу
                InsertMusicInfo(filePath); //вызов метода для добавления информации о файле в базу данных
                ShowTable(fullTable);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    //Внесение данных о выбранном файле
    private void InsertMusicInfo(string filePath)
    {
        try
        {
            _connection.Open();
            
            TagLib.File file = TagLib.File.Create(filePath); //загрузка файла
            TagLib.Tag tag = file.Tag; //получение информации о тегах

            // Извлечение информации об исполнителе, альбоме и жанре, в случае отуствии таких данных выставляется значение "Неизвестно"
            string artist = tag.FirstArtist ?? "Неизвестно";
            string album = tag.Album ?? "Неизвестно";
            string genre = tag.Genres.Length > 0 ? tag.Genres[0] : "Неизвестно";

            // Создание команды для добавления данных в базу данных
            using (var command = new NpgsqlCommand())
            {
                command.Connection = _connection;
                command.CommandText = "INSERT INTO Музыка (Название, Исполнитель, Альбом, Жанр, Путь) VALUES (@name, @artist, @album, @genre, @path)";
                command.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(filePath));
                command.Parameters.AddWithValue("@artist", artist);
                command.Parameters.AddWithValue("@album", album);
                command.Parameters.AddWithValue("@genre", genre);
                command.Parameters.AddWithValue("@path", filePath);
                command.ExecuteNonQuery(); //выполнение команды
            }
            _connection.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    
    // Ручное внесение данных по нажатию кнопки
    private void AddData_Click(object? sender, RoutedEventArgs e)
    {
        MusicClass newMusic = new MusicClass(); //создание нового экземпляр класса MusicClass
        AddEditWindow add = new AddEditWindow(newMusic, mus); //открытие окна добавления/редактирования с новым экземпляром класса
        add.Show();
        this.Hide(); //скрываем текущее окно
    }

    // Изменение данных выбранной записи по нажатию кнопки
    private void EditData_Click(object? sender, RoutedEventArgs e)
    {
        MusicClass currenMusic = DataGrid.SelectedItem as MusicClass; //получаем выбранный элемент из DataGrid
        if (currenMusic == null) //если элемент не выбран, выходим из метода
            return;
        AddEditWindow edit = new AddEditWindow(currenMusic, mus); //открытие окна добавления/редактирования с выбранным экземпляром класса
        edit.Show();
        this.Hide(); // Скрываем текущее окно
    }

    // Реализация поиска по вводу значения в текстовое поле
    private void SearchMusic(object? sender, TextChangedEventArgs e)
    {
        var name = mus; //получаем список всех элементов из коллекции mus
        name = name.Where(x => x.Name.Contains(Search_Music.Text)).ToList(); //фильтруем список по введенному значению в поле поиска
        DataGrid.ItemsSource = name; //обновление источника данных DataGrid отфильтрованным списком
    }

    // Обработчик события загрузки строки в DataGrid для окрашивания строк с нужным значением
    private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        MusicClass music = e.Row.DataContext as MusicClass; //получаем экземпляр класса MusicClass из контекста строки
        if (music != null) // Если экземпляр не пустой проверяем поля на наличие значения "Неизвестно"
        {
            if (music.Artist == "Неизвестно" || music.Album == "Неизвестно" || music.Genre == "Неизвестно") //проверка поля на наличие значения "Неизвестно"
            {
                e.Row.Background = Brushes.DarkSlateGray; // Если хотя бы одно поле имеет значение "Неизвестно", устанавливаем для строки определенный фон
            }
            else
            {
                e.Row.Background = Brushes.Transparent; // Если все поля заполнены, устанавливаем для строки прозрачный фон
            }
        }
    }
    
    private void Artist_Filter_OnClick(object? sender, SelectionChangedEventArgs e)
    {
        var ComboBox = (ComboBox)sender;
        var currentMusic = ComboBox.SelectedItem as MusicClass;
        var filteredArtist = mus
            .Where(x => x.Artist == currentMusic.Artist)
            .ToList();
        DataGrid.ItemsSource = filteredArtist;
    }

    public void FillStatus()
    {
        mus = new List<MusicClass>();
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        NpgsqlCommand command = new NpgsqlCommand("SELECT * FROM Музыка ORDER BY Исполнитель", _connection); //создание команды для выполнения запроса
        NpgsqlDataReader reader = command.ExecuteReader(); //выполнение запроса и получение результата
        while (reader.Read() && reader.HasRows) //чтение результатов запроса
        {
            var Music = new MusicClass()
            { 
                //получение результатов столбцов по классу 
                Name = reader.GetString(0),
                Artist = reader.GetString(1),
                Album = reader.GetString(2),
                Genre = reader.GetString(3),
                Path = reader.GetString(4)
            };
            if (!mus.Any(x => x.Artist == Music.Artist)) //проверка, существует ли объект MusicClass с таким же значением поля "Исполнитель" в списке mus
            {
                mus.Add(Music); //добавление объекта MusicClass в список mus.
            }
        }
        _connection.Close();
        var genderComboBox = this.Find<ComboBox>("CmbArtist");
        genderComboBox.ItemsSource = mus.DistinctBy(x => x.Artist); //использование метода DistinctBy для удаления дубликатов по полю "Исполнитель"
    }
    
    // Сброс фильтрации и поиска
    private void Reset_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowTable(fullTable); //отображение всех записей 
        Search_Music.Text = string.Empty; //сделать строку поиска пустой
    }
}
