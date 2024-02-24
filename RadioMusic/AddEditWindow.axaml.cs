using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using System;

namespace RadioMusic;

public partial class AddEditWindow : Window
{
    private NpgsqlConnection _connection; //поле для подключения к базе данных
    private List<MusicClass> Mus; //список музыкальных композиций
    private MusicClass CurrenMusic; //текущая редактируемая композиция
    string connectionString = "Server=localhost;Port=5432;Database=RadioMusic;User Id=postgres;Password=123456;";

    public AddEditWindow(MusicClass currenMusic, List<MusicClass> mus)
    {
        InitializeComponent();
        CurrenMusic = currenMusic;
        this.DataContext  = currenMusic; //установка в поля значений выбранного элемента
        Mus = mus;
    }
    
   private void Save_OnClick(object? sender, RoutedEventArgs e)
{
    var user = Mus.FirstOrDefault(x => x.Name == CurrenMusic.Name); //gолучаем композицию из списка по названию
    // Если композиция не найдена, добавляем ее в базу данных
    if (user == null)
    {
        try
        {
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            // Использование запроса с параметрами для предотвращения проблем с одинарными кавычками
            string add = "INSERT INTO Музыка (Название, Исполнитель, Альбом, Жанр, Путь) VALUES (@name, @artist, @album, @genre, @path);";
            NpgsqlCommand cmd = new NpgsqlCommand(add, _connection);
            cmd.Parameters.AddWithValue("name", Name.Text);
            cmd.Parameters.AddWithValue("artist", Artist.Text);
            cmd.Parameters.AddWithValue("album", Album.Text);
            cmd.Parameters.AddWithValue("genre", Genre.Text);
            cmd.Parameters.AddWithValue("path", Path.Text);

            cmd.ExecuteNonQuery();
            _connection.Close();
        }
        catch (Exception exception)
        {
            Console.WriteLine("Ошибка: " + exception);
        }
    }
    else //если запись найдена, выполняется редактирование информации по названию трека
    {
        try
        {
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            // Использование запроса с параметрами для предотвращения проблем с одинарными кавычками
            string upd = "UPDATE Музыка SET Исполнитель = @artist, Альбом = @album, Жанр = @genre, Путь = @path WHERE Название = @name;";
            NpgsqlCommand cmd = new NpgsqlCommand(upd, _connection);
            cmd.Parameters.AddWithValue("name", Name.Text);
            cmd.Parameters.AddWithValue("artist", Artist.Text);
            cmd.Parameters.AddWithValue("album", Album.Text);
            cmd.Parameters.AddWithValue("genre", Genre.Text);
            cmd.Parameters.AddWithValue("path", Path.Text);

            cmd.ExecuteNonQuery();
            _connection.Close();
        }
        catch (Exception exception)
        {
            Console.WriteLine("Ошибка: " + exception);
        }
    }
}
   
    private void GoBack_OnClick(object? sender, RoutedEventArgs e)
    {
        MainWindow back = new MainWindow();
        this.Close();
        back.Show(); 
    }
}