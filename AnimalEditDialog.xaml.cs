using AnimalShelterAI.Core.DTOs;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimalShelterAI
{
    public partial class AnimalEditDialog : Window
    {
        private readonly AnimalDto? _animalDto;
        private readonly AnimalService _animalService;
        private animal? _existingAnimal;
        private bool _isEditMode;
        private bool _isLoading = false;
        private string? _selectedPhotoPath = null;
        private byte[]? _selectedPhotoBytes = null;

        public AnimalEditDialog(AnimalDto? animalDto = null)
        {
            InitializeComponent();
            _animalService = new AnimalService(new ShelterDbContext());
            _animalDto = animalDto;
            _isEditMode = animalDto != null;

            Title = _isEditMode ? "Редактирование животного" : "Новое животное";
            txtTitle.Text = _isEditMode ? "Редактирование животного" : "Добавление животного";
            btnSave.Content = _isEditMode ? "Сохранить изменения" : "Добавить животное";

            Loaded += AnimalEditDialog_Loaded;
        }

        private async void AnimalEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTypesAsync();
            LoadStatusesAndHealthAsync();

            if (_isEditMode && _animalDto != null)
            {
                await LoadExistingAnimalAsync();
            }
            else
            {
                dpAdmissionDate.SelectedDate = DateTime.Today;
                cmbStatus.SelectedIndex = 1;
                cmbHealthStatus.SelectedIndex = 0;

                cmbBreed.Items.Clear();
                cmbBreed.Items.Add(new ComboBoxItem { Content = "Сначала выберите вид", Tag = null });
                cmbBreed.SelectedIndex = 0;

                // Устанавливаем фото по умолчанию
                SetDefaultPhoto();
            }
        }

        private void SetDefaultPhoto()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/picture.png", UriKind.RelativeOrAbsolute);
                if (uri != null)
                {
                    imgPhoto.Source = new BitmapImage(uri);
                }
            }
            catch
            {
                // Если картинки нет, показываем иконку
                imgPhoto.Source = null;
            }
        }

        private async Task LoadTypesAsync()
        {
            try
            {
                var types = await _animalService.GetAnimalTypesAsync();

                cmbType.Items.Clear();
                cmbType.Items.Add(new ComboBoxItem { Content = "Выберите вид", Tag = 0 });

                foreach (var t in types)
                    cmbType.Items.Add(new ComboBoxItem { Content = t.typename, Tag = t.typeid });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов: {ex.Message}", "Ошибка");
            }
        }

        private async Task LoadBreedsAsync(int typeId)
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;

                cmbBreed.Items.Clear();
                cmbBreed.Items.Add(new ComboBoxItem { Content = "Без породы", Tag = null });

                var breeds = await _animalService.GetBreedsByTypeAsync(typeId);

                foreach (var breed in breeds)
                {
                    cmbBreed.Items.Add(new ComboBoxItem { Content = breed.breedname, Tag = breed.breedid });
                }

                if (cmbBreed.Items.Count > 0)
                    cmbBreed.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пород: {ex.Message}", "Ошибка");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadStatusesAndHealthAsync()
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add(new ComboBoxItem { Content = "Карантин", Tag = "Quarantine" });
            cmbStatus.Items.Add(new ComboBoxItem { Content = "Ищет дом", Tag = "Available" });
            cmbStatus.Items.Add(new ComboBoxItem { Content = "Забронирован", Tag = "Reserved" });
            cmbStatus.Items.Add(new ComboBoxItem { Content = "Усыновлён", Tag = "Adopted" });
            cmbStatus.Items.Add(new ComboBoxItem { Content = "На лечении", Tag = "Treatment" });

            cmbHealthStatus.Items.Clear();
            cmbHealthStatus.Items.Add(new ComboBoxItem { Content = "Здоров", Tag = "Healthy" });
            cmbHealthStatus.Items.Add(new ComboBoxItem { Content = "Болен", Tag = "Sick" });
            cmbHealthStatus.Items.Add(new ComboBoxItem { Content = "Восстанавливается", Tag = "Recovering" });
            cmbHealthStatus.Items.Add(new ComboBoxItem { Content = "Хроническое", Tag = "Chronic" });
        }

        private async Task LoadExistingAnimalAsync()
        {
            _existingAnimal = await _animalService.GetFullAnimalByIdAsync(_animalDto!.Animalid);

            if (_existingAnimal == null)
            {
                MessageBox.Show("Животное не найдено", "Ошибка");
                Close();
                return;
            }

            txtName.Text = _animalDto.Name ?? "";
            txtAge.Text = _animalDto.Age?.ToString() ?? "";
            txtWeight.Text = _animalDto.Weight?.ToString("0.##") ?? "";
            txtColor.Text = _animalDto.Color ?? "";
            txtDescription.Text = _animalDto.Description ?? "";
            dpAdmissionDate.SelectedDate = _animalDto.Admissiondate;

            // Загружаем фото если есть
            if (!string.IsNullOrEmpty(_animalDto.Photourl))
            {
                LoadPhotoFromUrl(_animalDto.Photourl);
            }
            else
            {
                SetDefaultPhoto();
            }

            // Выбираем вид
            foreach (ComboBoxItem item in cmbType.Items)
            {
                if (item.Tag is int typeId && typeId == _existingAnimal.typeid)
                {
                    cmbType.SelectedItem = item;
                    break;
                }
            }

            // Загружаем породы после выбора вида
            if (_existingAnimal.typeid > 0)
            {
                await LoadBreedsAsync(_existingAnimal.typeid);

                if (_existingAnimal.breedid.HasValue)
                {
                    foreach (ComboBoxItem item in cmbBreed.Items)
                    {
                        if (item.Tag is int breedId && breedId == _existingAnimal.breedid.Value)
                        {
                            cmbBreed.SelectedItem = item;
                            break;
                        }
                    }
                }
            }

            rbMale.IsChecked = _animalDto.Gender == "Male";
            rbFemale.IsChecked = _animalDto.Gender == "Female";

            SelectComboItem(cmbStatus, _animalDto.Status);
            SelectComboItem(cmbHealthStatus, _animalDto.Healthstatus);
        }

        private void LoadPhotoFromUrl(string photoUrl)
        {
            try
            {
                if (File.Exists(photoUrl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgPhoto.Source = bitmap;
                    _selectedPhotoPath = photoUrl;
                    BtnRemovePhoto.Visibility = Visibility.Visible;
                }
                else if (photoUrl.StartsWith("data:image"))
                {
                    // Base64 изображение
                    var base64Data = photoUrl.Substring(photoUrl.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64Data);
                    LoadPhotoFromBytes(bytes);
                }
                else
                {
                    SetDefaultPhoto();
                }
            }
            catch
            {
                SetDefaultPhoto();
            }
        }

        private void LoadPhotoFromBytes(byte[] bytes)
        {
            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(bytes))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }
                imgPhoto.Source = bitmap;
                _selectedPhotoBytes = bytes;
                BtnRemovePhoto.Visibility = Visibility.Visible;
            }
            catch
            {
                SetDefaultPhoto();
            }
        }

        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Title = "Выберите фото животного"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем изображение
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imgPhoto.Source = bitmap;
                    _selectedPhotoPath = dialog.FileName;
                    _selectedPhotoBytes = File.ReadAllBytes(dialog.FileName);
                    BtnRemovePhoto.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "Ошибка");
                }
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            SetDefaultPhoto();
            _selectedPhotoPath = null;
            _selectedPhotoBytes = null;
            BtnRemovePhoto.Visibility = Visibility.Collapsed;
        }

        private void PhotoBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BtnSelectPhoto_Click(sender, e);
        }

        private async void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            if (cmbType.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int typeId && typeId > 0)
            {
                await LoadBreedsAsync(typeId);
            }
            else
            {
                if (!_isLoading)
                {
                    cmbBreed.Items.Clear();
                    cmbBreed.Items.Add(new ComboBoxItem { Content = "Сначала выберите вид", Tag = null });
                    cmbBreed.SelectedIndex = 0;
                }
            }
        }

        private void SelectComboItem(ComboBox combo, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var item = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == value);
            if (item != null) combo.SelectedItem = item;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите кличку животного", "Ошибка");
                return;
            }

            if (cmbType.SelectedItem is not ComboBoxItem typeItem || (int)typeItem.Tag == 0)
            {
                MessageBox.Show("Выберите вид животного", "Ошибка");
                return;
            }

            try
            {
                // Сохраняем фото в папку приложения
                string? photoUrl = null;
                if (_selectedPhotoBytes != null)
                {
                    photoUrl = await SavePhotoToFile(_selectedPhotoBytes, txtName.Text.Trim());
                }
                else if (_selectedPhotoPath != null && File.Exists(_selectedPhotoPath))
                {
                    photoUrl = await SavePhotoToFile(File.ReadAllBytes(_selectedPhotoPath), txtName.Text.Trim());
                }

                var dto = new AnimalCreateDto
                {
                    Name = txtName.Text.Trim(),
                    Typeid = (int)((ComboBoxItem)cmbType.SelectedItem).Tag,
                    Breedid = (cmbBreed.SelectedItem as ComboBoxItem)?.Tag as int?,
                    Gender = rbMale.IsChecked == true ? "Male" : rbFemale.IsChecked == true ? "Female" : null,
                    Age = int.TryParse(txtAge.Text, out int a) ? a : null,
                    Weight = decimal.TryParse(txtWeight.Text, out decimal w) ? w : null,
                    Color = txtColor.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    Photourl = photoUrl,
                    Admissiondate = dpAdmissionDate.SelectedDate ?? DateTime.Today,
                    Status = ((ComboBoxItem)cmbStatus.SelectedItem)?.Tag?.ToString() ?? "Quarantine",
                    Healthstatus = ((ComboBoxItem)cmbHealthStatus.SelectedItem)?.Tag?.ToString() ?? "Healthy"
                };

                bool success = _isEditMode && _existingAnimal != null
                    ? await _animalService.UpdateAnimalAsync(_existingAnimal.animalid, dto)
                    : await _animalService.CreateAnimalAsync(dto);

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при сохранении животного", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private async Task<string?> SavePhotoToFile(byte[] photoBytes, string animalName)
        {
            try
            {
                // Создаём папку для фото если её нет
                string photosDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                if (!Directory.Exists(photosDir))
                {
                    Directory.CreateDirectory(photosDir);
                }

                // Генерируем уникальное имя файла
                string fileName = $"{animalName}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                string filePath = Path.Combine(photosDir, fileName);

                // Сохраняем файл
                await File.WriteAllBytesAsync(filePath, photoBytes);

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения фото: {ex.Message}", "Ошибка");
                return null;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.BorderBrush = string.IsNullOrWhiteSpace(tb.Text)
                    ? new SolidColorBrush(Colors.Red)
                    : new SolidColorBrush(Colors.Green);
            }
        }
    }
}