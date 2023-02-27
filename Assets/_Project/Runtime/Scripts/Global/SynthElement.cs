using System;
using Unity.Entities;
using UnityEngine;

using NaughtyAttributes;

namespace PlaneWaver
{
    /// <summary>
    /// Abstract class for managing synth elements
    /// </summary>
    public abstract class SynthElement : MonoBehaviour
    {
        #region CLASS DEFINITIONS

        public int EntityIndex { get; private set; }
        protected EntityManager Manager;
        protected EntityArchetype Archetype;
        protected Entity ElementEntity;
        protected SynthElementType ElementType = SynthElementType.Blank;
        protected bool EntityInitialised;

        #endregion

        #region INITIALISATION METHODS

        public void Awake()
        {
            ElementType = SynthElementType.Blank;
            EntityIndex = int.MaxValue;
            EntityInitialised = false;
        }
        
        protected void InitialiseEntity()
        {
            EntityIndex = GrainBrain.Instance.RegisterEntity(this, ElementType);
            ElementEntity = Manager.CreateEntity(Archetype);
            name = $"{Enum.GetName(typeof(SynthElementType), ElementType)}.{EntityIndex}";
#if UNITY_EDITOR
            Manager.SetName(ElementEntity, name);
#endif
            InitialiseComponents();
            EntityInitialised = true;
        }
        
        protected virtual void InitialiseComponents() { }
        
        #endregion

        #region ENTITY UPDATE LOOP

        public void PrimaryUpdate()
        {
            if (!EntityInitialised)
                return;

            ProcessComponents();
        }

        protected virtual void ProcessComponents() { }

        #endregion

        #region ENTIY MANAGEMENT

        private void OnDestroy()
        {
            DestroyEntity();
        }

        private void DestroyEntity()
        {
            Deregister();
            try
            {
                if (World.All.Count != 0 && Manager.Exists(ElementEntity))
                    Manager.DestroyEntity(ElementEntity);
            }
            catch (Exception ex) when (ex is NullReferenceException)
            {
                //Debug.Log($"Failed to destroy entity: {ex.Message}");
            }
        }

        protected virtual void Deregister() { }

        #endregion
    }

    #region TYPE DEFINITIONS

    public enum SynthElementType { Blank, Speaker, Host, Emitter, Frame };

    #endregion
}