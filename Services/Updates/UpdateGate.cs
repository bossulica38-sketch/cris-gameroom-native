using System.Windows;
using CrisGameRoom.Models;

namespace CrisGameRoom.Services.Updates;

public static class UpdateGate
{
    public static async Task<bool> EnsureCurrentAsync(Window owner)
    {
        var service = new GameRoomUpdateService();

        UpdateInfo? update;

        try
        {
            update = await service.CheckAsync();
        }
        catch
        {
            return true;
        }

        if (update is null)
            return true;

        var result = MessageBox.Show(
            owner,
            $"Este disponibilă versiunea {update.Version}.\n\n" +
            "Este necesară actualizarea aplicației înainte de continuare.\n\n" +
            update.ReleaseNotes,
            "Actualizare Jocuri și Discuții",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            if (update.Mandatory)
            {
                MessageBox.Show(
                    owner,
                    "Actualizarea este obligatorie pentru a continua.",
                    "Actualizare necesară",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        try
        {
            var progress = new Progress<double>();

            service.StartUpdater(update);

            // Updaterul a preluat controlul. Procesul aplicației trebuie
            // să se închidă complet înainte ca EXE-ul să fie înlocuit.
            Application.Current.Shutdown();
            Environment.Exit(0);
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                owner,
                "Actualizarea nu a putut fi instalată.\n\n" + ex.Message,
                "Actualizare nereușită",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return update.Mandatory == false;
        }
    }
}
