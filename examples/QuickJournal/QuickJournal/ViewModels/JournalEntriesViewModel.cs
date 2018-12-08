using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Realms;
using Realms.Sync;
using Xamarin.Forms;

namespace QuickJournal
{
    public class JournalEntriesViewModel : INotifyPropertyChanged
    {
        // TODO: add UI for changing that.
        private const string AuthorName = "Me";

        private Realm _realm;

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<JournalEntry> Entries { get; private set; }

        public ICommand AddEntryCommand { get; private set; }

        public ICommand DeleteEntryCommand { get; private set; }

        public INavigation Navigation { get; set; }

        public JournalEntriesViewModel()
        {
            AddEntryCommand = new Command(AddEntry);
            DeleteEntryCommand = new Command<JournalEntry>(DeleteEntry); 


            //Use Local Database

            //_realm = Realm.GetInstance();
            //Entries = _realm.All<JournalEntry>();
            //RaisePropertyChanged(nameof(Entries));


            //Use data sync

            GetRealmInstance().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    Debug.WriteLine("Fail");
                }
            });

        }

        public void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async Task GetRealmInstance()
        {
            var serverip = "13.88.24.115";

            if (User.AllLoggedIn.Count() > 0)
            {
                foreach (var u in User.AllLoggedIn.AsEnumerable())
                {
                    u.LogOut();
                }
            }
            User user = User.Current;

            if (user == null)
            {
                var creds = Credentials.UsernamePassword("bar@bar.com", "test2", false);

                var authUri = new Uri($"http://{serverip}:9080");

                //without this the error won't propogate back up
                try
                {
                    user = await User.LoginAsync(creds, authUri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine((ex.Message));
                }
            }

            var config = new SyncConfiguration(user, new Uri($"realm://{serverip}:9080/~/journal")); //~ indicates per user realm

            _realm = Realm.GetInstance(config);

            Entries = _realm.All<JournalEntry>();
            RaisePropertyChanged(nameof(Entries));

        }


        private void AddEntry()
        {
            var transaction = _realm.BeginWrite();
            var entry = _realm.Add(new JournalEntry
            {
                Metadata = new EntryMetadata
                {
                    Date = DateTimeOffset.Now,
                    Author = AuthorName
                }
            });

            var page = new JournalEntryDetailsPage(new JournalEntryDetailsViewModel(entry, transaction));

            Navigation.PushAsync(page);
        }

        internal void EditEntry(JournalEntry entry)
        {
            var transaction = _realm.BeginWrite();

            var page = new JournalEntryDetailsPage(new JournalEntryDetailsViewModel(entry, transaction));

            Navigation.PushAsync(page);
        }

        private void DeleteEntry(JournalEntry entry)
        {
            _realm.Write(() => _realm.Remove(entry));
        }
    }
}

