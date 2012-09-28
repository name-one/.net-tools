using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools.Testing
{
    /// <summary>
    /// Emulates behavior of a database table with an auto-increment identity field.
    /// </summary>
    /// <typeparam name="T">
    /// Specifies the type of items that can be contained in the <see cref="IdentityTableEmulator{T}"/>.
    /// </typeparam>
    public class IdentityTableEmulator<T> : ICollection<T>
    {
        private readonly Dictionary<int, T> _contents = new Dictionary<int, T>();
        private readonly IdGetter _getId;
        private readonly PropertyInfo _idProperty;
        private readonly IdSetter _setId;
        private int _identity;

        public IdentityTableEmulator()
        {
            // Determine how to set id for the item type.
            Type type = typeof(T);

            // Use IIdentityModel interface if it is implemented by the item type.
            if (type.GetInterfaces().Contains(typeof(IIdentityModel)))
            {
                _getId = GetModelId;
                _setId = SetModelId;
                return;
            }

            // Try to find and use Int32 Id property of the item type.
            _idProperty = type.GetProperty("Id", typeof(int));
            if (_idProperty != null)
            {
                _getId = GetInt32Id;
                _setId = SetInt32Id;
                return;
            }

            // Try to find and use Int64 Id property of the item type.
            _idProperty = type.GetProperty("Id", typeof(long));
            if (_idProperty != null)
            {
                _getId = GetInt64Id;
                _setId = SetInt64Id;
                return;
            }

            throw new NotSupportedException(
                "The item type must either implement IIdentityModel interface or have a public Int32 or Int64 property named \"Id\"");
        }

        /// <summary>
        /// Gets the id value of the specified item.
        /// </summary>
        /// <param name="item">Item to get id of.</param>
        /// <returns>
        /// Id of the item.
        /// </returns>
        private delegate int IdGetter(T item);

        /// <summary>
        /// Sets an id value to the specified item.
        /// </summary>
        /// <param name="item">Item to set id to.</param>
        /// <param name="id">Id to set.</param>
        private delegate void IdSetter(T item, int id);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="IdentityTableEmulator{T}"/>.
        /// </summary>
        public int Count
        {
            get { return _contents.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds an item to the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="IdentityTableEmulator{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="IdentityTableEmulator{T}"/>.</param>
        /// <returns>
        /// Identity value of the added item.
        /// </returns>
        public int Add(T item)
        {
            lock (_contents)
            {
                ++_identity;
                _setId(item, _identity);
                _contents.Add(_identity, item);
                return _identity;
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="IdentityTableEmulator{T}"/>.
        /// </summary>
        public void Clear()
        {
            lock (_contents)
            {
                _contents.Clear();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="IdentityTableEmulator{T}"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="item"/> is found in the <see cref="IdentityTableEmulator{T}"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="IdentityTableEmulator{T}"/>.</param>
        public bool Contains(T item)
        {
            return _contents.ContainsValue(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="IdentityTableEmulator{T}"/> to an <see cref="Array"/>,
        /// starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="IdentityTableEmulator{T}"/>. The <see cref="Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex"> The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _contents.Values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            return _contents.Values.GetEnumerator();
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IdentityTableEmulator{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="IdentityTableEmulator{T}"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="item"/> was successfully removed from the
        /// <see cref="IdentityTableEmulator{T}"/>; otherwise, <c>false</c>.
        /// This method also returns <c>false</c> if <paramref name="item"/> is not found
        /// in the original <see cref="IdentityTableEmulator{T}"/>.
        /// </returns>
        public bool Remove(T item)
        {
            return _contents.Remove(_getId(item));
        }

        /// <summary>
        /// Gets the id value of the specified item, provided that the item has an <see cref="Int32"/> property
        /// named "Id".
        /// </summary>
        /// <param name="item">Item to get id of.</param>
        /// <returns>
        /// Id of the item.
        /// </returns>
        private int GetInt32Id(T item)
        {
            return (int)_idProperty.GetValue(item, null);
        }

        /// <summary>
        /// Gets the id value of the specified item, provided that the item has an <see cref="Int64"/> property
        /// named "Id".
        /// </summary>
        /// <param name="item">Item to get id of.</param>
        /// <returns>
        /// Id of the item.
        /// </returns>
        private int GetInt64Id(T item)
        {
            return (int)((long)_idProperty.GetValue(item, null));
        }

        /// <summary>
        /// Gets the id value of the specified item, provided that the item implements
        /// <see cref="IIdentityModel"/> interface.
        /// </summary>
        /// <param name="item">Item to get id of.</param>
        /// <returns>
        /// Id of the item.
        /// </returns>
        private int GetModelId(T item)
        {
            return ((IIdentityModel)item).Id;
        }

        /// <summary>
        /// Sets an id value to the specified item, provided that the item has an <see cref="Int32"/> property
        /// named "Id".
        /// </summary>
        /// <param name="item">Item to set id to.</param>
        /// <param name="id">Id to set.</param>
        private void SetInt32Id(T item, int id)
        {
            _idProperty.SetValue(item, id, null);
        }

        /// <summary>
        /// Sets an id value to the specified item, provided that the item has an <see cref="Int64"/> property
        /// named "Id".
        /// </summary>
        /// <param name="item">Item to set id to.</param>
        /// <param name="id">Id to set.</param>
        private void SetInt64Id(T item, int id)
        {
            _idProperty.SetValue(item, (long)id, null);
        }

        /// <summary>
        /// Sets an id value to the specified item, provided that the item implements
        /// <see cref="IIdentityModel"/> interface.
        /// </summary>
        /// <param name="item">Item to set id to.</param>
        /// <param name="id">Id to set.</param>
        private void SetModelId(T item, int id)
        {
            ((IIdentityModel)item).Id = id;
        }
    }
}