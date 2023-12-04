using VContainer;
using VContainer.Unity;
using UnityEngine;
using SpellBound.Core;
using System.Collections.Generic;

public class PlayerControllerLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<PlayerController>();
    }
}