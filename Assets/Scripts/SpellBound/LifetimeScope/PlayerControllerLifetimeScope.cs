using VContainer;
using VContainer.Unity;
using UnityEngine;

public class PlayerControllerLifetimeScope : LifetimeScope
{
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private GameObject bossEnemyPrefab;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<PlayerController>();
        builder.RegisterFactory<string, Vector3, GameObject>(container =>
        {
            return (kind, pos) =>
            {
                GameObject go;
                switch (kind)
                {
                    case "Boss":
                        go = Instantiate(this.bossEnemyPrefab, pos, Quaternion.identity);
                        break;
                    case "Warrior":
                        go = Instantiate(this.enemyPrefab, pos, Quaternion.identity);
                        break;
                    default:
                        Debug.LogError($"Unknown enemy kind {kind}");
                        return null;
                }

                container.InjectGameObject(go);
                return go;
            };
        }, Lifetime.Scoped);
    }
}