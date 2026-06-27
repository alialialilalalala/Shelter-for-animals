using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class DonationEditDialog : Window
    {
        private readonly donation? _donation;
        private readonly bool _isEditMode;

        public DonationEditDialog(donation? donation = null)
        {
            InitializeComponent();

            _donation = donation;
            _isEditMode = donation != null;

            if (_isEditMode)
            {
                txtWindowTitle.Text = "Редактирование пожертвования";
                btnSave.Content = "Сохранить изменения";
                Title = "Редактирование пожертвования - Приют";
                LoadDonationData();
            }
            else
            {
                txtWindowTitle.Text = "Новое пожертвование";
                btnSave.Content = "Добавить пожертвование";
                Title = "Новое пожертвование - Приют";
                dpDonationDate.SelectedDate = DateTime.Today;
            }
        }

        private void LoadDonationData()
        {
            dpDonationDate.SelectedDate = _donation!.donationDate;
            txtAmount.Text = _donation.amount.ToString("0.##");

            // Выбираем тип
            foreach (ComboBoxItem item in cmbDonationType.Items)
            {
                if (item.Content.ToString() == _donation.donationtype)
                {
                    cmbDonationType.SelectedItem = item;
                    break;
                }
            }

            txtDonorName.Text = _donation.donorname ?? "";
            chkIsAnonymous.IsChecked = _donation.isanonymous;
            txtNotes.Text = _donation.notes ?? "";

            // Если анонимно, отключаем поле имени
            if (_donation.isanonymous)
            {
                txtDonorName.IsEnabled = false;
                txtDonorName.Text = "";
            }
        }

        private void ChkIsAnonymous_Checked(object sender, RoutedEventArgs e)
        {
            txtDonorName.IsEnabled = false;
            txtDonorName.Text = "";
        }

        private void ChkIsAnonymous_Unchecked(object sender, RoutedEventArgs e)
        {
            txtDonorName.IsEnabled = true;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!dpDonationDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату пожертвования", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму пожертвования", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new ShelterDbContext();

                donation donation;
                if (_isEditMode && _donation != null)
                {
                    donation = await context.donations.FindAsync(_donation.donationid);
                    if (donation == null)
                    {
                        MessageBox.Show("Запись не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    donation = new donation();
                    context.donations.Add(donation);
                }

                donation.donationDate = dpDonationDate.SelectedDate.Value;
                donation.amount = amount;
                donation.donationtype = ((ComboBoxItem)cmbDonationType.SelectedItem)?.Content.ToString();
                donation.isanonymous = chkIsAnonymous.IsChecked == true;
                donation.donorname = donation.isanonymous ? null : txtDonorName.Text.Trim();
                donation.notes = txtNotes.Text.Trim();

                await context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}