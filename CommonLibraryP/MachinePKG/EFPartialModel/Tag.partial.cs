using CommonLibraryP.API;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommonLibraryP.MachinePKG
{
    public abstract partial class Tag
    {
        //param 1: station, param2: in/out put, param 3: start index param, param 4: offset
        public Tag()
        {
            //Init();
        }

        public Tag(Guid CategoryID)
        {
            Id = Guid.NewGuid();
            DataType = 1;
            CategoryId = CategoryID;
        }

        public void Init()
        {
            lastUpdateTime = DateTime.Now;
            lastChangedTime = DateTime.Now;
            InitVal();
        }

        [NotMapped]
        public bool IsMultipleValue => DataType > 10;
        [NotMapped]
        public bool IsBoolean => DataType % 10 == 1;
        [NotMapped]
        public bool IsUshort => DataType % 10 == 2;
        [NotMapped]
        public bool IsString => DataType % 10 == 4;


        private DateTime lastUpdateTime;
        public DateTime LastUpdateTime => lastUpdateTime;
        private DateTime lastChangedTime;
        public DateTime LastChangedTime => lastChangedTime;

        public Object Value => value;
        private Object value = new();
        public string ValueString => FormatingValueToString();

        public event Func<Tag, Task>? TagValueChanged;

        protected abstract void InitVal();
        

        public RequestResult SetValue(Object obj)
        {
            lastUpdateTime = DateTime.Now;
            if (MachineTypeEnumHelper.TypeMatch((int)DataType, obj.GetType()))
            {
                if (obj.GetType().IsArray)
                {
                    if (!StructuralComparisons.StructuralEqualityComparer.Equals(value, obj))
                    {
                        value = obj;
                        ValueChanged();
                        return new(1, $"Update tag {Name} success");
                    }
                    else
                    {
                        return new(1, $"Tag {Name} not changed");
                    }
                }
                else
                {
                    if (!value.Equals(obj))
                    {
                        value = obj;
                        ValueChanged();
                        return new(1, $"Update tag {Name} success");
                    }
                    else
                    {
                        return new(1, $"Tag {Name} not changed");
                    }
                }
            }
            else
            {
                return new(4, $"Update tag {Name} fail data type not match");
            }
        }

        private void ValueChanged()
        {
            lastChangedTime = DateTime.Now;
            if (TagValueChanged is not null)
            {
                foreach (var handler in TagValueChanged.GetInvocationList())
                {
                    try
                    {
                        Task.Run(() => ((Func<Tag, Task>)handler).Invoke(this));

                        //((Func<object, Task>)handler).Invoke(this);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"訂閱者執行失敗：{ex.Message}");
                    }
                }

                //TagValueChanged?.Invoke(this);
            }
            
        } 
        private string FormatingValueToString()
        {
            if (value == null)
                return string.Empty;
            if (value.GetType().IsArray)
            {
                if (value is IEnumerable valueEnum)
                {
                    return "[" + string.Join(",", valueEnum.Cast<Object>().Select(x=>x.ToString())) + "]";
                }
                else
                {
                    return string.Empty;
                }
                    
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
