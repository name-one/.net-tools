using System;
using System.ComponentModel;
using System.Reflection;

namespace InoSoft.Tools.Mvvm
{
    /// <summary>
    /// Base class for view models, which provides simple way to link them with models.
    /// </summary>
    /// <typeparam name="TSource">Type of source model, which view model will be linked with.</typeparam>
    public abstract class ViewModel<TSource> : INotifyPropertyChanged
    {
        private TSource _source;

        /// <summary>
        /// Creates ViewModel.
        /// </summary>
        /// <param name="source">Source auto-trader to link with.</param>
        public ViewModel(TSource source)
        {
            _source = source;
        }

        private event PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Property changed event.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// Gets source object, which is view model linked with.
        /// </summary>
        /// <remarks>
        /// Made in method fashion instead of property because view model may wish to use property named Source for another purpose. So, base class is property-free.
        /// </remarks>
        public TSource GetSource()
        {
            return _source;
        }

        /// <summary>
        /// Fetches data from source to view model.
        /// </summary>
        /// <remarks>
        /// This is base helper, which copies values from source to view model properties with MappedProperty attribute using reflection.
        /// If MappedProperty has defined template, data is converted to string, so corresponding view model property must have type of string.
        /// If any derived class needs fetching additional properties manually, Fetch method needs to be overriden and extended (with base method call).
        /// PropertyChanged event is fired automatically when MappedProperty attribute is used, but if fetch is manual then take care of firing event manually too.
        /// </remarks>
        public virtual void Fetch()
        {
            foreach (PropertyInfo prop in GetType().GetProperties())
            {
                MappedPropertyAttribute att = (MappedPropertyAttribute)Attribute.GetCustomAttribute(prop, typeof(MappedPropertyAttribute));
                if (att != null)
                {
                    PropertyInfo sourceProp = _source.GetType().GetProperty(att.SourceName ?? prop.Name);
                    if (sourceProp != null)
                    {
                        object oldValue = prop.GetValue(this, null);
                        object newValue;
                        if (att.Template != null)
                        {
                            newValue = sourceProp.GetValue(_source, null);
                        }
                        else
                        {
                            newValue = String.Format(att.Template, sourceProp.GetValue(_source, null));
                        }

                        if (!object.Equals(oldValue, newValue))
                        {
                            prop.SetValue(this, newValue, null);
                            OnPropertyChanged(prop.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves data in view model to source.
        /// </summary>
        /// <remarks>
        /// This is base helper, which copies values back from view model properties with MappedProperty attribute to source object using reflection.
        /// Only properties with matching types will be applied.
        /// If any derived class needs applying additional properties manually, Apply method needs to be overriden and extended (with base method call).
        /// </remarks>
        public virtual void Apply()
        {
            foreach (PropertyInfo prop in GetType().GetProperties())
            {
                MappedPropertyAttribute att = (MappedPropertyAttribute)Attribute.GetCustomAttribute(prop, typeof(MappedPropertyAttribute));
                if (att != null)
                {
                    PropertyInfo sourceProp = _source.GetType().GetProperty(att.SourceName ?? prop.Name);
                    if (prop.PropertyType == sourceProp.PropertyType)
                    {
                        sourceProp.SetValue(_source, prop.GetValue(this, null), null);
                    }
                }
            }
        }

        /// <summary>
        /// Handles changement of property.
        /// </summary>
        /// <param name="name">Name of changed property.</param>
        protected void OnPropertyChanged(string name)
        {
            var propertyChanged = _propertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}