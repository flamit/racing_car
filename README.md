# racing_car
Dynamic damages to car

Physics of vehicle Damage

My role was to make AI on the physics and make a dynamic crash effect.
To make a crash effect I would need the Meshfilter and meshcollider classes, the mesh collider takes a mesh asset and builds a collider based on that mesh, the meshfilter is for shader effect.
First step is to store the initial positions and rotations of the mesh, for both the Meshfilter and Meshcollider. We also have to store the original vertices of the colliders and the  
During the game the car can undergo many crashes, so it is important that the mesh is dynamically stored for efficient memory usage as otherwise storing each vertex would cause the game to run much slower. In order to get around this we can mark the mesh as MarkDynamic(). 

When there is an impact this needs to be processed, first we have to check if there is an impact and if it is above a specified parameter otherwise merely driving the car would cause collisions and destructions. When there is an impact the data we need to take account of is the position, velocity of the impact and the vertices on the original Mesh. When there is an impact I didn't want the whole car to be wreck after one impact so the impact was constrained by a parameter, I would have a limiting factor on the radius of the impact. The nearer the impact becomes to the maximum damage radius the force proportionally decreases.      

Vector3 damage = (localContactForce * (damageRadius - Mathf.Sqrt(dist)) / damageRadius) 

There is a cut off if it is over the max damage radius.

	if (deform.sqrMagnitude > maxDisplacement * maxDisplacement)
				T.localPosition = originalLocalPos + deform.normalized * maxDisplacement;




I took account of the distortion of the wheel nodes in the same manner. Something more that can be done is to separate out each part of the car and then calculate the impact effect of each part separately. This may be effective for improving gameplay,

Here is the video of dynamic crash effect 

https://www.youtube.com/watch?v=BlYG-GHxAYI


To make the crash effect realistic there is also rotation of the nodes, based on the damage ratio I would increase the effect of rotation to give the appearance of a crumpling effect.

I combined the dynamic crash with the crash of the static crash mesh produced by the art team, so there were 2 layers of the cars mesh created one for initial stage with no crash and one that was deformed, I added the dynamic deformation part to the  deformed layer, so that more interesting crash effects can be constructed to the crash. 

The video here demonstrates the dynamic crash effect added to the artwork of the 2nd layer deformed mesh. 

https://www.youtube.com/watch?v=SxtII6M1mLg


Repair effect

I added a repair effect, the idea is that the car would undergo a pit stop to trigger the repair process, and the feature will thus improve gameplay. I had the original vertices stored, so its really just making the vertices move back to its original positions. 
