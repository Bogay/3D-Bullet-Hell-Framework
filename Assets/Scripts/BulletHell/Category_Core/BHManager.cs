﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using MessagePipe;
using VContainer.Unity;
using MessagePipe.VContainer;
using VContainer;

namespace BulletHell3D
{
    public class BHManager : MonoBehaviour
    {
        private static BHManager _instance = null;
        public static BHManager instance { get { return _instance; } }
        private const int renderMask = 31;

        [SerializeField]
        private bool debug = false;

        [Space(10), SerializeField]
        private BHRenderObject[] renderObjects;

        [Space(10), SerializeField]
        private bool useParticle;

        private LayerMask collisionMask;
        private LayerMask obstacleMask;
        private LayerMask playerMask;

        [Inject]
        private readonly IPublisher<System.Guid, CollisionEvent> collisionPublisher;
        [Inject]
        private readonly CollisionGroups collisionGroups;

        #region BulletUpdater

        private int totalBulletCount = 0;
        private BHRenderGroup[] renderGroups;
        private Dictionary<BHRenderObject, BHRenderGroup> object2Group = new Dictionary<BHRenderObject, BHRenderGroup>();
        private List<IBHBulletUpdater> bulletUpdaters = new List<IBHBulletUpdater>();
        private Queue<IBHBulletUpdater> addBulletQueue = new Queue<IBHBulletUpdater>();
        private Queue<IBHBulletUpdater> removeBulletQueue = new Queue<IBHBulletUpdater>();

        private NativeArray<RaycastHit> bulletResults = default;
        private NativeArray<SpherecastCommand> bulletCommands = default;
        private JobHandle bulletHandle;

        #endregion

        #region RayUpdater

        private List<IBHRayUpdater> rayUpdaters = new List<IBHRayUpdater>();
        private Queue<IBHRayUpdater> addRayQueue = new Queue<IBHRayUpdater>();
        private Queue<IBHRayUpdater> removeRayQueue = new Queue<IBHRayUpdater>();

        private NativeArray<RaycastHit> rayResults = default;
        private NativeArray<SpherecastCommand> rayCommands = default;
        private JobHandle rayHandle;

        #endregion

        public void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void Start()
        {
            obstacleMask = this.collisionGroups.obstacleMask;
            playerMask = this.collisionGroups.playerMask;
            collisionMask = obstacleMask | playerMask | this.collisionGroups.enemyMask;

            // Initialize rendering params.
            renderGroups = new BHRenderGroup[renderObjects.Length];
            for (int i = 0; i < renderObjects.Length; i++)
            {
                renderGroups[i] = new BHRenderGroup(renderObjects[i]);
                object2Group.Add(renderObjects[i], renderGroups[i]);
            }
        }

        void OnGUI()
        {
            if (debug)
                GUI.Label(new Rect(5, 5, 200, 30), "Instance Count: " + totalBulletCount.ToString());
        }

        //TODO: Might need a function that can wipe out all currently existing bullets. (For example: Boss killed)
        public void FixedUpdate()
        {
            UpdateBullets();
            //Focus on bullets only right now.
            //UpdateRays();
        }

        private void UpdateBullets()
        {
            totalBulletCount = 0;

            #region Check Alive

            if (bulletResults.IsCreated)
            {
                // Wait for the batch processing job to complete
                bulletHandle.Complete();

                // Update bullets' info by the result of sphere casts.
                int i = 0;
                foreach (IBHBulletUpdater updatable in bulletUpdaters)
                {
                    var list = updatable.bullets;
                    foreach (BHBullet bullet in list)
                    {
                        if (bulletResults[i].collider != null)
                        {
                            int colliderLayer = 1 << (bulletResults[i].collider.gameObject.layer);
                            bullet.isAlive = false;
                            // play particle system on collision
                            if (useParticle)
                                BHParticlePool.instance.RequestParticlePlay(bulletResults[i].point);
                            this.collisionPublisher.Publish(bullet.groupId, new CollisionEvent
                            {
                                bullet = bullet,
                                contact = bulletResults[i].collider.gameObject,
                                hit = bulletResults[i],
                            });
                        }
                        i++;
                    }
                }

                // Dispose the buffers
                bulletResults.Dispose();
                bulletCommands.Dispose();
            }

            Debug.Assert(!bulletResults.IsCreated);
            Debug.Assert(!bulletCommands.IsCreated);

            #endregion

            #region Add/Remove Updatables

            while (addBulletQueue.Count != 0)
            {
                var updatable = addBulletQueue.Dequeue();
                bulletUpdaters.Add(updatable);
            }
            while (removeBulletQueue.Count != 0)
            {
                var updatable = removeBulletQueue.Dequeue();
                if (bulletUpdaters.Contains(updatable))
                    bulletUpdaters.Remove(updatable);
            }

            #endregion

            #region Update Position And Matrix

            //Update positions
            float deltaTime = Time.fixedDeltaTime;
            foreach (IBHBulletUpdater updatable in bulletUpdaters)
            {
                updatable.RemoveBullets();
                updatable.UpdateBullets(deltaTime);
            }

            //Update render group
            foreach (BHRenderGroup group in renderGroups)
                group.InvalidateOldBatchedRenderData();
            foreach (IBHBulletUpdater updatable in bulletUpdaters)
            {
                var list = updatable.bullets;
                foreach (BHBullet bullet in list)
                {
                    BHRenderGroup group = object2Group[bullet.renderObject];
                    // TODO: Support color modification at runtime.
                    group.SetBulletRenderData(bullet.position);
                }
            }
            foreach (BHRenderGroup group in renderGroups)
                group.FinalizeBatchedRenderData();

            #endregion

            #region Collision Detection

            foreach (IBHBulletUpdater updatable in bulletUpdaters)
                totalBulletCount += updatable.bullets.Count;

            if (totalBulletCount == 0)
                return;

            // Set up the command buffers
            bulletResults = new NativeArray<RaycastHit>(totalBulletCount, Allocator.TempJob);
            bulletCommands = new NativeArray<SpherecastCommand>(totalBulletCount, Allocator.TempJob);

            // Set the data of sphere cast commands
            {
                int i = 0;
                foreach (IBHBulletUpdater updatable in bulletUpdaters)
                {
                    var list = updatable.bullets;
                    foreach (BHBullet bullet in list)
                    {
                        bulletCommands[i] = new SpherecastCommand
                        (
                            bullet.position - bullet.delta,
                            bullet.renderObject.radius,
                            bullet.delta.normalized,
                            new QueryParameters(
                                collisionMask,
                                true, QueryTriggerInteraction.UseGlobal, false
                            ),
                            bullet.delta.magnitude
                        );
                        i++;
                    }
                }
            }

            // Schedule the batch of sphere casts
            bulletHandle = SpherecastCommand.ScheduleBatch(bulletCommands, bulletResults, 256);

            #endregion     
        }

        //TODO: This really feels like duplication of code, which is kinda bad. (Violates DRY principle.)
        //Might need a better way to implement this. 
        /*
        private void UpdateRays()
        {
            int totalRayCount = 0;
            int counter = 0;

            //This region is still not complete yet.
            #region Check Alive

            if(rayResults.IsCreated)
            {
                // Wait for the batch processing job to complete
                rayHandle.Complete();

                // Update rays' info by the result of sphere casts.
                counter = 0;
                foreach(IBHRayUpdater updatable in rayUpdaters)
                {
                    var list = updatable.rays;
                    foreach(BHRay ray in list)
                    {
                        if(rayResults[counter].collider != null)
                        {
                            int colliderLayer = 1 << (rayResults[counter].collider.gameObject.layer);
                            if((colliderLayer | playerMask) != 0)
                            {
                                Debug.Log("Hit Player");
                            }
                        }
                        counter ++;
                    }
                }

                // Dispose the buffers
                rayResults.Dispose();
                rayCommands.Dispose();
            }

            #endregion

            #region Add/Remove Updatables

            while(addRayQueue.Count != 0)
            {
                var updatable = addRayQueue.Dequeue();
                rayUpdaters.Add(updatable);
            }
            while(removeRayQueue.Count != 0)
            {
                var updatable = removeRayQueue.Dequeue();
                if(rayUpdaters.Contains(updatable))
                    rayUpdaters.Remove(updatable);
            }

            #endregion
        
            #region Update Rays & Collision Detection

            float deltaTime = Time.fixedDeltaTime;
            foreach(IBHRayUpdater updatable in rayUpdaters)
                updatable.UpdateRays(deltaTime);

            foreach(IBHRayUpdater updatable in rayUpdaters)
                totalRayCount += updatable.rays.Length;

            // Set up the command buffers
            rayResults = new NativeArray<RaycastHit>(totalRayCount, Allocator.Persistent);
            rayCommands = new NativeArray<SpherecastCommand>(totalRayCount, Allocator.Persistent);

            // Set the data of sphere cast commands
            counter = 0;
            foreach(IBHRayUpdater updatable in rayUpdaters)
            {
                var list = updatable.rays;
                foreach(BHRay ray in list)
                {
                    // Player
                    rayCommands[counter] = new SpherecastCommand
                    (
                        ray.origin,
                        updatable.rayRadius,
                        ray.normalizedDirection,
                        ray.length,
                        collisionMask
                    );

                    counter ++;
                }
            }

            // Schedule the batch of sphere casts
            rayHandle = SpherecastCommand.ScheduleBatch(rayCommands, rayResults, 256, default(JobHandle));

            #endregion
        }
        */

        public void Update()
        {
            RenderBullets();
        }

        private void RenderBullets()
        {
            foreach (BHRenderGroup group in renderGroups)
            {
                for (int i = 0; i < group.batchCount; i++)
                {
                    Graphics.DrawMeshInstanced
                    (
                        group.renderObject.mesh,
                        0,
                        group.renderObject.material,
                        group.batches[i].matrices,
                        (i == group.batchCount - 1) ? group.count % BHRenderGroup.RENDER_NUM_PER_BATCH : BHRenderGroup.RENDER_NUM_PER_BATCH,
                        group.batches[i].propertyBlock,
                        UnityEngine.Rendering.ShadowCastingMode.Off,
                        false,
                        30
                    );
                }
            }
        }

        public void AddUpdatable(IBHUpdater updatable)
        {
            if (updatable is IBHBulletUpdater)
                addBulletQueue.Enqueue(updatable as IBHBulletUpdater);
            else if (updatable is IBHRayUpdater)
                addRayQueue.Enqueue(updatable as IBHRayUpdater);
        }

        public void RemoveUpdatable(IBHUpdater updatable)
        {
            if (updatable is IBHBulletUpdater)
                removeBulletQueue.Enqueue(updatable as IBHBulletUpdater);
            else if (updatable is IBHRayUpdater)
                removeRayQueue.Enqueue(updatable as IBHRayUpdater);
        }

        private void OnDisable()
        {
            if (bulletResults.IsCreated) bulletResults.Dispose();
            if (bulletCommands.IsCreated) bulletCommands.Dispose();
            if (rayResults.IsCreated) rayResults.Dispose();
            if (rayCommands.IsCreated) rayCommands.Dispose();
        }
    }
}