using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
//using SQLite;

namespace LastMileHealth.Models
{
    //[Table("Zips")]
    public class Zip : INotifyPropertyChanged
    {
        private bool isSelected;

        //[PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public DateTime CreationDate { get; set; }

        public string FullPath { get; set; }

        public string FolderPath { get; set; }

        //[Ignore]
        public bool IsSelected
        {
            get 
            { 
                return this.isSelected; 
            }

            set
            {
                this.isSelected = value;
                this.RaisePropertyChanged("IsSelected");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
