using System;

namespace NinOS.Domain
{
    public class relacion
    {
        private int _id_relacion;
        private int _relation_number;
        private DateTime _week_start;
        private DateTime _week_end;

        public int id_relacion
        {
            get { return _id_relacion; }
            set
            {
                if (value < 0) throw new ArgumentException();
                _id_relacion = value;
            }
        }

        public int relation_number
        {
            get { return _relation_number; }
            set
            {
                if (value <= 0) throw new ArgumentException();
                _relation_number = value;
            }
        }

        public DateTime week_start
        {
            get { return _week_start; }
            set
            {
                if (value == default) throw new ArgumentException();
                _week_start = value.Date;
            }
        }

        public DateTime week_end
        {
            get { return _week_end; }
            set
            {
                if (value == default) throw new ArgumentException();
                _week_end = value.Date;
            }
        }

        protected relacion()
        {
        }

        public relacion(int relation_number, DateTime week_start, DateTime week_end)
        {
            this.relation_number = relation_number;
            this.week_start = week_start;
            this.week_end = week_end;
        }
    }
}
