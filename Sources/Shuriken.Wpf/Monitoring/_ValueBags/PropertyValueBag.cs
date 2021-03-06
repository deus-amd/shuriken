using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Shuriken.Diagnostics;

namespace Shuriken.Monitoring
{
    internal sealed class PropertyValueBag : ValueBag
    {
        [NotNull]
        readonly PropertyPropertyAccessor propertyAccessor;

        volatile object currentValue;

        volatile object newValue;

        volatile bool isValueValid;

        volatile bool isValueChanged;

        internal PropertyValueBag([NotNull] ObservableObject observableObject, [NotNull] PropertyPropertyAccessor propertyAccessor)
        {
            this.propertyAccessor = propertyAccessor;

            try
            {
                currentValue = propertyAccessor.Getter(observableObject);
                isValueValid = true;
            }
            catch (Exception e)
            {
                EventSource.Log.UnableInitiallyToReadProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
            }
        }

        public override bool HasValidValue => isValueValid;

        public override bool HasChangedValue => isValueChanged;

        public override void UpdateNewValue(ObservableObject observableObject)
        {
            try
            {
                newValue = propertyAccessor.Getter(observableObject);
                isValueValid = true;
            }
            catch (Exception e)
            {
                if (isValueValid)
                {
                    EventSource.Log.UnableSubsequentlyToReadProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
                }

                isValueValid = false;
            }
        }

        public override void AnalyzeNewValue()
        {
            Debug.Assert(isValueValid);

            if (propertyAccessor.UseReferenceEquality)
            {
                isValueChanged = !ReferenceEquals(currentValue, newValue);
            }
            else
            {
                try
                {
                    isValueChanged = !Equals(currentValue, newValue);
                }
                catch (Exception e)
                {
                    EventSource.Log.UnableToAnalyzeProperty(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
                    isValueChanged = false;
                }
            }

            if (isValueChanged)
            {
                currentValue = newValue;
            }
            newValue = null;
        }

        public override void NotifyPropertyChanged(ObservableObject observableObject)
        {
            Debug.Assert(isValueChanged);

            try
            {
                observableObject.NotifyPropertyChange(propertyAccessor.Name);
            }
            catch (Exception e)
            {
                EventSource.Log.UnableToRaisePropertyChangeNotification(propertyAccessor.ObjectTypeName, propertyAccessor.Name, e.ToString());
            }
            finally
            {
                isValueChanged = false;
            }
        }
    }
}