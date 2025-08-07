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
        [NotMapped]
        public bool ConditionWarningAllow => !IsMultipleValue && (IsBoolean || IsUshort);

        private DateTime lastUpdateTime;
        public DateTime LastUpdateTime => lastUpdateTime;
        private DateTime lastChangedTime;
        public DateTime LastChangedTime => lastChangedTime;

        public Object Value => value;
        private Object value = new();
        public string ValueString => FormatingValueToString();

        public bool HasWarning => TagWarningConditions.Count() > 0;

        public bool HasWarningTriggered => TagWarningConditions.Count(x => x.IsWarning) > 0;

        public int WarningCount => TagWarningConditions.Count(x => x.IsWarning);

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
            CheckAllWarnings();
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
                        //Console.WriteLine($"訂閱者執行失敗：{ex.Message}");
                    }
                }

                //TagValueChanged?.Invoke(this);
            }
            
        }

        private void CheckAllWarnings()
        {
            if (Value is null || TagWarningConditions == null || !TagWarningConditions.Any())
                return;

            foreach (var condition in TagWarningConditions)
            {
                try
                {
                    var targetValueStr = condition.TargetValueString;
                    bool isWarning = condition.ComparisonCode switch
                    {
                        (int)LogicalOperation.Equal => SafeEquals(Value, targetValueStr),
                        (int)LogicalOperation.NotEqual => !SafeEquals(Value, targetValueStr),
                        (int)LogicalOperation.Large => SafeCompare(Value, targetValueStr) > 0,
                        (int)LogicalOperation.Less => SafeCompare(Value, targetValueStr) < 0,
                        (int)LogicalOperation.LargerThanOrEqualTo => SafeCompare(Value, targetValueStr) >= 0,
                        (int)LogicalOperation.LessThanOrEqualTo => SafeCompare(Value, targetValueStr) <= 0,
                        _ => false
                    };

                    if (isWarning)
                    {
                        condition.TriggerWarning();
                    }
                    else
                    {
                        condition.DismissWarning();
                    }
                }
                catch (Exception ex)
                {
                }
            }


        }

        private bool SafeEquals(object value, string target)
        {
            if (value is bool boolVal && bool.TryParse(target, out var boolTarget))
                return boolVal == boolTarget;

            if (value is ushort ushortVal && ushort.TryParse(target, out var ushortTarget))
                return ushortVal == ushortTarget;

            // fallback 比對
            return value.ToString() == target;
        }

        private int SafeCompare(object value, string target)
        {
            try
            {
                if (value is IComparable comparable)
                {
                    object? convertedTarget = Convert.ChangeType(target, value.GetType());
                    if (convertedTarget is IComparable comparableTarget)
                        return comparable.CompareTo(comparableTarget);
                }
            }
            catch
            {
                // Ignore failed conversions
            }

            return string.Compare(value?.ToString(), target, StringComparison.Ordinal);
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
