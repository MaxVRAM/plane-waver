using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

using NaughtyAttributes;

namespace PlaneWaver
{
    /// <summary>
    /// Abstract class for managing synth entities
    /// </summary>

    public abstract class SynthEntity : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        protected EntityManager _EntityManager;
        protected EntityArchetype _Archetype;
        protected Entity _Entity;

        [AllowNesting]
        [Foldout("Entity Status")]
        [ShowNonSerializedField] protected SynthEntityType _EntityType = SynthEntityType.Blank;
        [Foldout("Entity Status")]
        [ShowNonSerializedField] protected int _EntityIndex = int.MaxValue;
        [Foldout("Entity Status")]
        [ShowNonSerializedField] protected bool _EntityInitialised = false;
        [Foldout("Entity Status")]
        [ShowNonSerializedField] protected bool _ManagerInitialised = false;
        public int EntityIndex => _EntityIndex;

        public void Awake()
        {
            _EntityType = SynthEntityType.Blank;
            _EntityIndex = int.MaxValue;
            _EntityInitialised = false;
            _ManagerInitialised = false;
        }

        #endregion

        #region PRIMARY ENTITY UPDATE LOOP

        public bool PrimaryUpdate()
        {
            if (_EntityType == SynthEntityType.Blank)
                return false;

            if (!ManagerReady())
                return false;

            if (!EntityReady())
                return false;

            ProcessComponents();
            return true;
        }

        public virtual void InitialiseComponents() { }
        public virtual void ProcessComponents() { }

        #endregion

        #region ENTIY MANAGEMENT

        public void SetIndex(int index)
        {
            _EntityIndex = index;
            SetEntityType();
            name = $"{Enum.GetName(typeof(SynthEntityType), _EntityType)}.{_EntityIndex}";
            SetEntityName();
        }

        public virtual void SetEntityType() { }

        public bool SetEntityName()
        {
#if UNITY_EDITOR
            if (_Entity == Entity.Null)
                return false;

            if (_EntityManager.GetName(_Entity) != name)
            {
                _EntityManager.SetName(_Entity, name);
                return false;
            }
#endif
            return true;
        }

        public bool ManagerReady()
        {
            if (_EntityManager == null || _Archetype == null)
            {
                _ManagerInitialised = false;
                _EntityInitialised = false;
                return false;
            }
            if (!_ManagerInitialised)
            {
                _ManagerInitialised = true;
                _EntityInitialised = false;
                return false;
            }
            return true;
        }

        public bool EntityReady()
        {
            if (!_EntityInitialised)
            {
                if (!ManagerReady())
                    return false;

                if (EntityIndex == int.MaxValue)
                    return false;

                if (_Entity == Entity.Null)
                {
                    _Entity = _EntityManager.CreateEntity(_Archetype);
                    return false;
                }

                if (!SetEntityName())
                    return false;

                InitialiseComponents();
                _EntityInitialised = true;
                return false;
            }
            else return true;
        }

        private void OnDestroy()
        {
            DestroyEntity();
        }

        public void DestroyEntity()
        {
            Deregister();
            try
            {
                if (_EntityManager != null && World.All.Count != 0 && _Entity != null)
                    _EntityManager.DestroyEntity(_Entity);
            }
            catch (Exception ex) when (ex is NullReferenceException)
            {
                //Debug.Log($"Failed to destroy entity: {ex.Message}");
            }
        }

        public virtual void Deregister() { }

        #endregion
    }

    #region TYPE DEFINITIONS

    public static class SynthComponentType
    {
        public readonly static string Connection = "_AttachmentParameters";
        public readonly static string Windowing = "_WindowingBlob";
        public readonly static string AudioTimer = "_AudioTimer";
        public readonly static string AudioClip = "_AudioClip";

        public static bool IsValid(string entityType)
        {
            foreach (FieldInfo field in typeof(SynthComponentType).GetFields())
                if (field.GetValue(null).ToString() == entityType)
                    return true;
            Debug.Log($"Could not update entity of unknown type {entityType}");
            return false;
        }
    }

    public enum SynthEntityType { Blank, Speaker, Host, Emitter };

    #endregion
}
