<div align=center>
  
# Simple Unity Road Generator
*Ideal for quick, Low-Poly roads / Creating roads with LODs*

The Unity Road Generator is a simple tool designed to simplify the process of creating dynamic roads within Unity. This tool enables you to define a path using a series of control points, generating a fully textured road mesh along that path. It includes features for road dimensions, texture mapping, and smoothing.


[demo video](https://www.youtube.com/watch?v=Qne5CmUYiUQ)


## Key Features
**Road Dimensions:** Customize the road width, height, ground offset, and side extrusion to achieve the desired appearance.
<br>
**Texture Mapping:** Apply a road material with adjustable road painting distancing to control the texture's stretching along the road.
<br>
**Road Smoothing:** Smooth the generated road path to create more organic and visually pleasing curves.
<br>
**Ground Clipping Elimination:** Automatically adjust road height to avoid intersections with the terrain.

</div>

## Usage
1. Attach the `RoadsGenerator` script to an empty GameObject.
2. Define the road path using child GameObjects as control points.
3. Adjust parameters such as road width, material, and smoothing in the inspector.
4. Click the "Generate" button to create the road mesh.
5. Optionally, use the "Preview path" button to visualize and adjust the smoothing of the road.
6. The tool automatically eliminates (most) ground clipping to ensure the road conforms to the terrain.

## Important Note
After generating the road mesh, it is recommended to rename the generated GameObject to your preference, as the script will delete the old GameObject named "Road_Generated" when generating a new one.

## Instructions for Use
1. **Generate Button:** Click to create the road mesh based on the defined path and parameters.
2. **Preview Path Button:** Visualize and adjust the smoothing of the road path.
3. **Clear Smooth Points Button:** Remove all smoothing points added during the preview.

## Debug Mode
Enable debug mode to visualize additional information, such as vertex indices and path lines, aiding in troubleshooting and fine-tuning.

Feel free to explore and modify the provided script to suit specific project requirements. For further details, check the code comments for insights into the tool's implementation.



