﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace BulletHell3D
{
    /*
    // A potential problem. (Not something buggy or critically flawed, but something that needs to think it over carefully.)
    // Currently, this system only support basic bullet. (AKA, the bullet without trail effct)
    // Which is fine, but this could be a problem if want to add tracer bullet. (AKA, the bullet with fancy trail effct)
    // From what I've thought of, there are two ways to deal with it:
    //
    // Solution A:
    // Create a new Manager (Ex: BHTracerManager, or something like that.)
    //
    // Solution B:
    // Find a way to somehow make basic and tracer bullets co-exist within this Manager. (Two different update pipeline.)
    //
    // Both could (and should) work, with their own pros and cons.
    // The real problem lies within the REUSEABILITY of the code. (AKA, BHRenderObject, BHBullet, and BHUpdateable)
    // The main difference between basic and tracer bullets: (besides their visuals)
    //
    // * Tracer bullet is an actual gameobject. (In order to support trail rendering.)
    // * Basic bullet is just a "rendering instance".
    //
    // Besides this difference, they are actually quite alike. (They are both compatible under the definition of BHBullet)
    // And I would probably use the old BH-classes when implementing tracer bullets.
    // So technically I should choose Solution B in order to improve code reuseability.
    // But dear god having two separate update pipeline in the same class is just pure havoc.
    */

    /*
    // For me from the past, (AKA, the comment above) I think I've found a way that's agreeable for both of us.
    // We should stick with solution B, but not by trying to cram both of the pipeline implementation into this class.
    // Instead, we can extract the flow of pipeline and turn it into an interface.
    // With the newly created interface, we can implement two concrete pipeline class. (One for basic, one for tracer)
    // And the job of this manager? Execute the pipeline interface without knowing the exact process.
    // God damn I'm a genius. (I hope I won't get slapped by myself from future.)
    // Anyway, if future me or somebody else has a better solution, feel free to write it down!
    // By Me 2/14/2022
    */

    /*
    // You know what, past me? I shall slap your face in person.
    // It turns out, we dont need two explicit pipelines. In fact, the BHManager can stay as it is.
    // You see, the only problem with the tracer bullet is the trail rendering itself.
    // So why don't we create a object pool JUST for trail renderer?
    // This way, we can reuse the existing pipeline to render both base and tracer bullets.
    // The only difference is that tracer bullet have trail renderer following its position.
    // (That is, the "main body" of the tracer bullet is rendered by GPU instancing, only its trail is a gameobject.)
    // By Me 2/16/2022
    */

    public class BHManager : MonoBehaviour
    {
        private class BHRenderGroup
        {
            public BHRenderObject renderObject;
            public Matrix4x4[] matrices = new Matrix4x4[1023];
            public int count = 0;

            public const int maxBulletCount = 1023;
        }

        private static BHManager _instance = null;
        public static BHManager instance { get { return _instance; } }
        private const int renderMask = 31;

        [SerializeField]
        private BHRenderObject[] renderObjects;

        [Space(10), SerializeField]
        private bool useParticle;

        private LayerMask collisionMask;
        private LayerMask obstacleMask;
        private LayerMask playerMask;

        private BHRenderGroup[] renderGroups;
        private Dictionary<BHRenderObject,BHRenderGroup> object2Group = new Dictionary<BHRenderObject, BHRenderGroup>();
        private List<IBHBulletUpdater> updatables = new List<IBHBulletUpdater>();
        private Queue<IBHBulletUpdater> addQueue = new Queue<IBHBulletUpdater>();
        private Queue<IBHBulletUpdater> removeQueue = new Queue<IBHBulletUpdater>();

        public void Awake() 
        {
            if(_instance == null)
                _instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }    
        }

        public void Start() 
        {
            // Setup layer mask that we want to check collision with.
            // Basically, we want to check whether we have collision with player or walls.
            CollisionGroups collisionGroups = CollisionGroups.instance;
            obstacleMask = collisionGroups.obstacleMask;
            playerMask = collisionGroups.playerMask;
            collisionMask = obstacleMask | playerMask;

            // Initialize rendering params.
            renderGroups = new BHRenderGroup[renderObjects.Length];
            for(int i = 0; i < renderObjects.Length; i++)
            {
                renderGroups[i] = new BHRenderGroup() { renderObject = renderObjects[i] };
                object2Group.Add(renderGroups[i].renderObject, renderGroups[i]);
                
                Matrix4x4 initialMatrix = Matrix4x4.Scale(Vector3.one * renderGroups[i].renderObject.radius * 2);
                for(int j = 0; j < 1023; j ++)
                    renderGroups[i].matrices[j] = initialMatrix;
            }   
        }

        //TODO: Might need a function that can wipe out all currently existing bullets. (For example: Boss killed)
        public void FixedUpdate() 
        {
            UpdateBullets();
        }

        private NativeArray<RaycastHit> results = default;
        private NativeArray<SpherecastCommand> commands = default;
        private JobHandle rayHandle;

        private void UpdateBullets()
        {
            int totalBulletCount = 0;
            int counter = 0;

            #region Check Alive

            if(results.IsCreated)
            {
                // Wait for the batch processing job to complete
                rayHandle.Complete();

                // Update bullets' info by the result of sphere casts.
                counter = 0;
                foreach(IBHBulletUpdater updatable in updatables)
                {
                    var list = updatable.bullets;
                    foreach(BHBullet bullet in list)
                    {
                        if(results[counter].collider != null)
                        {
                            int colliderLayer = 1 << (results[counter].collider.gameObject.layer);

                            bullet.isAlive = false;
                            if(useParticle)
                                BHParticlePool.instance.RequestParticlePlay(results[counter].point);

                            //TODO: Make an event responds to specific layer hit. (for example: player's layer)
                            if((colliderLayer | obstacleMask) != 0)
                            {
                                // Bullet hit obstacle
                            }
                            if((colliderLayer | playerMask) != 0)
                            {
                                // Bullet hit player
                            }
                        }
                        counter++;
                    }
                }

                // Dispose the buffers
                results.Dispose();
                commands.Dispose();
            }

            #endregion

            #region Add/Remove Updatables

            while(addQueue.Count != 0)
            {
                var updatable = addQueue.Dequeue();
                updatables.Add(updatable);
            }
            while(removeQueue.Count != 0)
            {
                var updatable = removeQueue.Dequeue();
                if(updatables.Contains(updatable))
                    updatables.Remove(updatable);
            }

            #endregion

            #region Update Position And Matrix

            //Update positions
            float deltaTime = Time.fixedDeltaTime;
            foreach(IBHBulletUpdater updatable in updatables)
            {
                updatable.RemoveBullets();
                updatable.UpdateBullets(deltaTime);
            }

            //Update matrices
            foreach(BHRenderGroup group in renderGroups)
                group.count = 0;
            foreach(IBHBulletUpdater updatable in updatables)
            {
                var list = updatable.bullets;
                foreach(BHBullet bullet in list)
                {
                    BHRenderGroup group = object2Group[bullet.renderObject];
                    group.matrices[group.count].SetColumn(3, new Vector4(bullet.position.x, bullet.position.y, bullet.position.z, 1));
                    group.count++;
                }
            }

            #endregion

            #region Collision Detection

            foreach(IBHBulletUpdater updatable in updatables)
                totalBulletCount += updatable.bullets.Count;

            // Set up the command buffers
            results = new NativeArray<RaycastHit>(totalBulletCount, Allocator.Persistent);
            commands = new NativeArray<SpherecastCommand>(totalBulletCount, Allocator.Persistent);

            // Set the data of sphere cast commands
            counter = 0;
            foreach(IBHBulletUpdater updatable in updatables)
            {
                var list = updatable.bullets;
                foreach(BHBullet bullet in list)
                {
                    // Ok, SpherecastCommand can go to hell.
                    // The "distance" param in the SpherecastCommand DOES NOT REPRESENT THE ACTUAL RAYLENGTH.
                    // It only act as a scalar, the ACTUAL RAYLENGTH = the magnitude of the direction vector * "distance" param.
                    // And guess what unity document says about this?
                    // Nothing, literally NOTHING.
                    // Wow, just wow.
                    // Fxxk this shit.  
                    commands[counter] = new SpherecastCommand
                    (
                        bullet.position - bullet.delta,
                        bullet.renderObject.radius,
                        bullet.delta,
                        1,
                        collisionMask
                    );
                    counter++;
                }
            }

            // Schedule the batch of sphere casts
            rayHandle = SpherecastCommand.ScheduleBatch(commands, results, 256, default(JobHandle));

            #endregion     
        }

        public void Update()
        {
            Render();
        }

        private void Render()
        {
            foreach(BHRenderGroup group in renderGroups)
            {
                Graphics.DrawMeshInstanced
                (
                    group.renderObject.mesh,
                    0,
                    group.renderObject.material,
                    group.matrices,
                    group.count, 
                    null, 
                    UnityEngine.Rendering.ShadowCastingMode.Off, 
                    false,
                    renderMask
                );
            }
        }

        public void AddUpdatable(IBHBulletUpdater updatable)
        {
            addQueue.Enqueue(updatable);
        }

        public void RemoveUpdatable(IBHBulletUpdater updatable)
        {
            removeQueue.Enqueue(updatable);
        }
    }
}