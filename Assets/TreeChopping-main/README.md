# TreeChopping
Tree chopping system for unity.

Created using unity 6.

Adds and removes tree colliders within a radius of the player as he/she moves around your game world.
Upon felling a tree, a prefab of the same treee will be instantiated, a collider and rigidbody be added and configured and the original tree is removed from the terrain.

To Use:

Remove the tree colliders from your terrain tree prefabs, This is important. you MUST do this.

Add an empty game object to the scene and add the ColliderManager script to it.
Add the prefab in the inspector of the ColliderManager script.
Add the player to the ColliderManager script in the inspector or add a method to ColliderManager to automatically find the player.

Add the prefab to a layer your axe script or whatever you are using to raycast against the trees when you go to chop them down.

You'll need code to hook into the  public void TakeDamage(float amount) method of TreeCollider.
Something like:

 if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f, attackLayer))
        {
            var tree = hit.collider.GetComponent<TreeCollider>();
            if (tree != null)
            {
                tree.TakeDamage(1f); 
            }
        }

will do it.



I commented out all calls to my XP system, inventory system and sound manager system. Feel free to delete them.
