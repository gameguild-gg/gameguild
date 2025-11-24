# Path Smoothing

Once the pathfinding algorithm is done, the agent should follow the path as closely as possible. But the path is not always smooth. In this case, we can apply path smoothing to make the path more smooth.

## Follow path algorithm

The follow path algorithm is simple. The agent have to go to the next node in the path. When the agent is close enough to the target node, it should move to the next node. If the agent has reached the last node in the path, it should stop.

```c++
// I am assuming the path includes the start and the end nodes
void FollowPath(std::vector<Node> path)
{
    // find the closest center point to the agent
    size_t closestCenterId = FindClosestCenter(agent.position, path);

    // the next target will be the next entry in the path, if it is not the last node
    if (closestCenterId + 1 < path.size())
        // go to the next target
        Node nextTarget = path[closestCenterId + 1];
    else
        // go to the last target
        Node nextTarget = path[closestCenterId];

    // move the agent towards the next target
    agent.position = MoveTowards(agent.position, nextTarget.position, agent.speed * deltaTime);
}
```

You may have noticed that this algorithm is not very smooth and the agent makes sharp turns. To make the path smoother, we can use other interpolation methods. One of the most common methods is splines.

## Splines

::: info

This is an AI class, not animation class. So we will only cover the quadratic Bezier spline.

:::

<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/3/3d/B%C3%A9zier_2_big.gif/250px-B%C3%A9zier_2_big.gif" alt="Quadratic Bezier spline" />

WiP. For more details read the [wikipedia article](https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Quadratic_B%C3%A9zier_curves).

Here you can find the implementation of the quadratic Bezier spline to generate more samples so the path will be smoother, and the agent wont make sharp turns.

```c++
Point2D quadraticBezier(const Point2D& startPoint, const Point2D& controlPoint, const Point2D& endPoint, float interpolationFactor) {
    // Calculate complementary interpolation factor (1 - t)
    float complementaryFactor = 1.0f - interpolationFactor;

    // Pre-calculate squared factors for efficiency
    float interpolationSquared = interpolationFactor * interpolationFactor;
    float complementarySquared = complementaryFactor * complementaryFactor;

    // Quadratic Bezier formula: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
    // The curve starts at P₀, is pulled toward P₁, and ends at P₂
    Point2D result;
    result.x = complementarySquared * startPoint.x +
               2 * complementaryFactor * interpolationFactor * controlPoint.x +
               interpolationSquared * endPoint.x;

    result.y = complementarySquared * startPoint.y +
               2 * complementaryFactor * interpolationFactor * controlPoint.y +
               interpolationSquared * endPoint.y;

    return result;
}

std::vector<Point2D> interpolate(std::vector<Point2D> path, size_t samplesPerSegment) {
    // Handle edge cases: empty or single-point paths can't be interpolated
    if (path.size() < 2) return path;

    // Ensure we have at least 1 sample per segment
    if (samplesPerSegment == 0) samplesPerSegment = 1;

    std::vector<Point2D> interpolatedPath;

    // Pre-allocate memory for efficiency
    // Formula: (number of segments) × (samples per segment) + 1 final point
    interpolatedPath.reserve((path.size() - 1) * samplesPerSegment + 1);

    // Always include the starting point
    interpolatedPath.push_back(path[0]);

    // Special case: For paths with only 2 points, use simple linear interpolation
    // since we need 3 points for quadratic Bezier
    if (path.size() == 2) {
        for (size_t sampleIndex = 1; sampleIndex < samplesPerSegment; ++sampleIndex) {
            float t = static_cast<float>(sampleIndex) / samplesPerSegment;

            // Linear interpolation: lerp(a, b, t) = a(1-t) + b(t)
            Point2D interpolatedPoint = path[0] * (1.0f - t) + path[1] * t;
            interpolatedPath.push_back(interpolatedPoint);
        }
        interpolatedPath.push_back(path[1]);
        return interpolatedPath;
    }

    // Main interpolation loop: process each segment of the original path
    // For each segment from path[i] to path[i+1], we use a 3-point sliding window:
    // - Current point (path[i]) as the start
    // - Next point (path[i+1]) as the control point (pulls the curve)
    // - Point after that (path[i+2]) as the end target
    for (size_t segmentIndex = 0; segmentIndex < path.size() - 1; ++segmentIndex) {
        Point2D currentPoint = path[segmentIndex];
        Point2D nextPoint = path[segmentIndex + 1];

        // For the last segment, reuse the endpoint since there's no path[i+2]
        Point2D pointAfterNext = (segmentIndex + 2 < path.size()) ?
                                  path[segmentIndex + 2] :
                                  path[segmentIndex + 1];

        // Generate the interpolated samples for this segment
        // Note: Start from 1 to avoid duplicating the current point (already added)
        for (size_t sampleIndex = 1; sampleIndex <= samplesPerSegment; ++sampleIndex) {
            // Calculate interpolation factor: ranges from (1/n) to 1.0
            float t = static_cast<float>(sampleIndex) / samplesPerSegment;

            // Apply quadratic Bezier curve
            Point2D interpolatedPoint = quadraticBezier(currentPoint, nextPoint, pointAfterNext, t);
            interpolatedPath.push_back(interpolatedPoint);
        }
    }

    return interpolatedPath;
}
```
