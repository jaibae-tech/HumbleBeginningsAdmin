ModuleSpec.md (Updated)
Module 2 – Latitude
Purpose

Generates a continuous latitude energy field representing large-scale solar energy distribution across the world.

This field serves as a climate driver, not a classification system.

Inputs

Latitude uses only map geometry + configuration values.

It does not depend on elevation, terrain shape, or noise layers.

1. Map Dimensions

Map Height → Defines gradient direction

South Edge → Warmest region

North Edge → Coldest region

Latitude energy decreases smoothly from south → north.

2. Seed Context

Used only for:

Optional global warp phase offset

No tile-scale noise is applied.

3. Configuration Values
Latitude Min 01 (Lmin)

Lower bound of latitude energy.

Layman’s explanation:
Prevents the far north from becoming unrealistically frozen.

Effects:

Raises minimum temperature potential

Softens extreme cold climates

Higher values → Milder world

Typical range: 0.10 – 0.25

Latitude Max 01 (Lmax)

Upper bound of latitude energy.

Layman’s explanation:
Prevents the far south from becoming unrealistically tropical.

Effects:

Caps maximum heating potential

Reduces extreme heat

Lower values → Cooler world overall

Typical range: 0.80 – 0.95

Curve Power

Controls gradient shaping.

Layman’s explanation:
Changes how quickly climate shifts from warm → cold.

Effects:

1.0 → Linear transition (default)

>1.0 → Expands mid-latitudes (more temperate world)

<1.0 → Stronger polar contrast

Safe tuning range: 0.8 – 1.5

Enable Global Warp

Applies a single smooth planetary skew.

Layman’s explanation:
Simulates that the map slice is not perfectly aligned to latitude lines.

Effects:

Introduces subtle diagonal bias

No local noise or fragmentation

Purely large-scale variation

Recommended default: Disabled

Warp Amplitude

Strength of global skew.

Layman’s explanation:
How tilted the climate appears.

Effects:

Small values → Almost invisible (preferred)

Large values → Obvious diagonal energy shift

Safe range: 0.01 – 0.05

Season Amp Min 01

Minimum seasonal variation.

Layman’s explanation:
How much climates change over time in warm regions.

Effects:

Higher values → Less stable climates

Lower values → Stable warm zones

Season Amp Max 01

Maximum seasonal variation.

Layman’s explanation:
How harsh seasonal swings become in cold regions.

Effects:

Higher values → Strong winters/summers

Lower values → Gentle seasonal change

Season Latitude Power

Controls how seasonal strength increases toward the north.

Layman’s explanation:
How quickly seasons become severe as climates cool.

Effects:

Higher → Strong northern seasonality

Lower → Uniform seasons

Outputs

Module 2 writes continuous fields into WorldArrays.

LatitudeEnergy01[]

Primary output.

Range → [Lmin … Lmax]

Meaning → Relative climate energy baseline

Spatial behavior → Smooth south → north gradient

Used later by:

Temperature modeling

Moisture systems

Habitat suitability logic

Ecology & bestiary rules

SeasonalAmplitude01[] (if enabled)

Secondary output.

Range → [SeasonAmpMin … SeasonAmpMax]

Meaning → Seasonal temperature swing potential

Spatial behavior → Stronger toward colder latitudes

Used later by:

Runtime seasonal modulation

Weather/climate severity logic

PNG Outputs
WorldPreview_02_LatitudeEnergy.png

Visualization of latitude energy field.

Characteristics:

Grayscale gradient

South = brighter (warmer)

North = darker (colder)

No bands or stripes expected

Purpose:

Debug validation only

Should appear visually simple

Stacked Preview Exports (if enabled)

Latitude energy may tint composite previews but does not introduce new terrain colors.

Tuning Guidelines (Practical)
To make the world warmer overall:

Increase Latitude Min 01

Increase Latitude Max 01

To make the world colder overall:

Decrease Latitude Max 01

Decrease Latitude Min 01

To expand temperate regions:

Increase Curve Power

To increase climate asymmetry:

Enable Global Warp

Keep amplitude small

To exaggerate seasons:

Increase Season Amp Max 01

To soften seasons:

Reduce Season Amp Max 01

Expected Visual Behavior

Correct output should show:

Perfectly smooth gradient

No visible noise texture

No contour banding

No abrupt transitions

If artifacts appear, the defect is almost always:

Quantization

Incorrect normalization

Accidental noise injection

Coordinate inversion