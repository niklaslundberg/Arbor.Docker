using System;
using System.Collections.Generic;

namespace Arbor.Docker;

public static class ResourceExtensions
{
    extension(IResourceReference resourceReference)
    {
        public IResourceReference DependsOn(IResourceReference dependsOn)
        {
            ArgumentNullException.ThrowIfNull(resourceReference);
            ArgumentNullException.ThrowIfNull(dependsOn);

            if (ReferenceEquals(resourceReference, dependsOn))
            {
                throw new InvalidOperationException("Cannot reference itself");
            }

            if (resourceReference is not ResourceNode treeNode)
            {
                throw new InvalidOperationException("Invalid reference");
            }

            if (dependsOn is not ResourceNode dependsOnNode)
            {
                throw new InvalidOperationException("Invalid reference");
            }

            treeNode.DependsOn.Add(dependsOnNode);

            return resourceReference;
        }

        public IResourceReference WaitFor(IResourceReference dependsOn)
        {
            ArgumentNullException.ThrowIfNull(resourceReference);
            ArgumentNullException.ThrowIfNull(dependsOn);

            if (ReferenceEquals(resourceReference, dependsOn))
            {
                throw new InvalidOperationException("Cannot reference itself");
            }

            if (resourceReference is not ResourceNode treeNode)
            {
                throw new InvalidOperationException("Invalid reference");
            }

            if (dependsOn is not ResourceNode dependsOnNode)
            {
                throw new InvalidOperationException("Invalid reference");
            }

            treeNode.DependsOnStarted.Add(dependsOnNode);

            return resourceReference;
        }
    }
}