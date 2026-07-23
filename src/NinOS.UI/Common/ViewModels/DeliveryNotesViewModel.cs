using System;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class DeliveryNotesViewModel : ViewModelBase
    {
        private readonly IDeliveryNoteService _delivery_note_service;
        
        private string _note_number = string.Empty;
        private string _status_message = string.Empty;

        public string note_number
        {
            get { return _note_number; }
            set
            {
                _note_number = value;
                on_property_changed();
            }
        }

        public string status_message
        {
            get { return _status_message; }
            set
            {
                _status_message = value;
                on_property_changed();
            }
        }

        public ICommand process_note_command { get; }

        public DeliveryNotesViewModel(IDeliveryNoteService delivery_note_service)
        {
            if (delivery_note_service == null)
            {
                throw new ArgumentNullException(nameof(delivery_note_service));
            }
            
            _delivery_note_service = delivery_note_service;
            process_note_command = new RelayCommand(execute_process_note, can_execute_process_note);
        }

        private bool can_execute_process_note(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(_note_number);
        }

        private async void execute_process_note(object? parameter)
        {
            try
            {
                delivery_note new_note = (delivery_note)Activator.CreateInstance(typeof(delivery_note), nonPublic: true)!;
                typeof(delivery_note).GetProperty("note_number")?.SetValue(new_note, _note_number);
                typeof(delivery_note).GetProperty("creation_date")?.SetValue(new_note, DateTime.Now);
                typeof(delivery_note).GetProperty("id_seller")?.SetValue(new_note, 1);
                typeof(delivery_note).GetProperty("id_customer")?.SetValue(new_note, 1);
                typeof(delivery_note).GetProperty("total_amount_usd")?.SetValue(new_note, 150.00m);

                note_detail new_detail = (note_detail)Activator.CreateInstance(typeof(note_detail), nonPublic: true)!;
                typeof(note_detail).GetProperty("id_product")?.SetValue(new_detail, 1);
                typeof(note_detail).GetProperty("quantity")?.SetValue(new_detail, 2);
                typeof(note_detail).GetProperty("unit_price_usd")?.SetValue(new_detail, 75.00m);
                typeof(note_detail).GetProperty("subtotal_usd")?.SetValue(new_detail, 150.00m);

                note_detail[] details_array = new note_detail[] { new_detail };

                await _delivery_note_service.create_delivery_note_async(new_note, details_array);
                
                status_message = "success";
                note_number = string.Empty;
            }
            catch (Exception ex)
            {
                status_message = ex.Message;
            }
        }
    }
}