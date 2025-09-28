#include <iostream>
#include <vector>
#include <random>
#include <cmath>
#include <fstream>
#include <string>
#include <algorithm>
#include <cstdlib>

struct Point {
    float x, y;
};

// Fast Poisson-disk sampling (Bridson)
std::vector<Point> generate_poisson_disk_points_fast(
    float width, float height,
    float min_dist,
    int k = 30)
{
    float cell_size = min_dist / std::sqrt(2.0f);
    int grid_width = static_cast<int>(std::ceil(width / cell_size));
    int grid_height = static_cast<int>(std::ceil(height / cell_size));

    std::vector<int> grid(grid_width * grid_height, -1);
    std::vector<Point> points;
    std::vector<int> active;

    std::random_device rd;
    std::mt19937 rng(rd());
    std::uniform_real_distribution<float> uni(0.0f, 1.0f);

    float min_dist_sq = min_dist * min_dist;

    float fx = uni(rng) * width;
    float fy = uni(rng) * height;
    points.push_back({fx, fy});
    int gx = static_cast<int>(fx / cell_size);
    int gy = static_cast<int>(fy / cell_size);
    grid[gx + gy * grid_width] = 0;
    active.push_back(0);

    while (!active.empty()) {
        int pos = rng() % active.size();
        int idx = active[pos];
        Point base = points[idx];
        bool found = false;

        for (int i = 0; i < k; ++i) {
            float theta = uni(rng) * 2.0f * M_PI;
            float radius = min_dist * (1.0f + uni(rng));
            float nx = base.x + radius * std::cos(theta);
            float ny = base.y + radius * std::sin(theta);

            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                continue;

            int ngx = static_cast<int>(nx / cell_size);
            int ngy = static_cast<int>(ny / cell_size);

            bool ok = true;
            int xmin = std::max(0, ngx - 2);
            int xmax = std::min(grid_width - 1, ngx + 2);
            int ymin = std::max(0, ngy - 2);
            int ymax = std::min(grid_height - 1, ngy + 2);

            for (int gy_i = ymin; gy_i <= ymax && ok; ++gy_i) {
                for (int gx_i = xmin; gx_i <= xmax && ok; ++gx_i) {
                    int n_idx = grid[gx_i + gy_i * grid_width];
                    if (n_idx != -1) {
                        Point p = points[n_idx];
                        float dx = nx - p.x;
                        float dy = ny - p.y;
                        if (dx * dx + dy * dy < min_dist_sq) {
                            ok = false;
                            break;
                        }
                    }
                }
            }

            if (ok) {
                points.push_back({nx, ny});
                int new_idx = points.size() - 1;
                grid[ngx + ngy * grid_width] = new_idx;
                active.push_back(new_idx);
                found = true;
                break;
            }
        }

        if (!found) {
            active.erase(active.begin() + pos);
        }
    }

    return points;
}

// Relaxed/fixed-count Poisson-disk generator
std::vector<Point> generate_poisson_disk_points_fixed_count(
    float width, float height,
    float min_dist,
    size_t target_count,
    int max_attempts = 10)
{
    std::vector<Point> points;
    float current_min_dist = min_dist;
    int attempts = 0;

    while (points.size() < target_count && attempts < max_attempts) {
        points = generate_poisson_disk_points_fast(width, height, current_min_dist);

        if (points.size() < target_count) {
            // Relax the minimum distance slightly to allow more points
            current_min_dist *= 0.9f;
        }
        attempts++;
    }

    // If we have more points than needed, randomly remove extras
    if (points.size() > target_count) {
        std::random_device rd;
        std::mt19937 rng(rd());
        std::shuffle(points.begin(), points.end(), rng);
        points.resize(target_count);
    }

    return points;
}

// Save points as OBJ
void save_points_obj(const std::vector<Point>& points, const std::string& filename) {
    std::ofstream ofs(filename);
    if (!ofs) {
        std::cerr << "Failed to open file: " << filename << std::endl;
        return;
    }

    for (const auto& p : points) {
        ofs << "v " << p.x << " " << p.y << " 0.0\n";
    }
}

// Argument parser
void parse_args(int argc, char** argv,
                float& width, float& height, float& min_dist,
                size_t& target_count, std::string& filename)
{
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        if (arg == "--width" && i + 1 < argc) width = std::atof(argv[++i]);
        else if (arg == "--height" && i + 1 < argc) height = std::atof(argv[++i]);
        else if (arg == "--mindist" && i + 1 < argc) min_dist = std::atof(argv[++i]);
        else if (arg == "--count" && i + 1 < argc) target_count = std::atoi(argv[++i]);
        else if (arg == "--output" && i + 1 < argc) filename = argv[++i];
        else std::cerr << "Unknown argument: " << arg << std::endl;
    }
}

int main(int argc, char** argv) {
    float width = 200.0f;
    float height = 200.0f;
    float min_dist = 1.0f;
    size_t target_count = 100;
    std::string filename = "points_fixed.obj";

    parse_args(argc, argv, width, height, min_dist, target_count, filename);

    auto points = generate_poisson_disk_points_fixed_count(width, height, min_dist, target_count);

    save_points_obj(points, filename);

    std::cout << "Generated " << points.size()
              << " Poisson-disk points in " << width << "x" << height
              << " aiming for target count " << target_count
              << " with min_dist=" << min_dist
              << " and saved to " << filename << std::endl;

    return 0;
}
