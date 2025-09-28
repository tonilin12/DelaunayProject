#include <iostream>
#include <vector>
#include <random>
#include <cmath>
#include <fstream>
#include <string>
#include <algorithm>
#include <limits>

struct Point {
    float x, y;
};

// Fast Poisson-disk sampling (Bridson) in C++
std::vector<Point> generate_poisson_disk_points_fast(
    float width, float height, float min_dist, float lam, int k = 30)
{
    float area = width * height;
    int max_points = std::max(1, static_cast<int>(lam * area));

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

    // first point
    float fx = uni(rng) * width;
    float fy = uni(rng) * height;
    points.push_back({fx, fy});
    int gx = static_cast<int>(fx / cell_size);
    int gy = static_cast<int>(fy / cell_size);
    grid[gx + gy * grid_width] = 0;
    active.push_back(0);

    while (!active.empty() && points.size() < static_cast<size_t>(max_points)) {
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
                for (int gx_i = xmin; gx_i <= xmax; ++gx_i) {
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

// Optional wrapper to save points
void save_points_binary(const std::vector<Point>& points, const std::string& filename) {
    std::ofstream ofs(filename, std::ios::binary);
    if (!ofs) {
        std::cerr << "Failed to open file: " << filename << std::endl;
        return;
    }
    uint32_t count = static_cast<uint32_t>(points.size());
    ofs.write(reinterpret_cast<const char*>(&count), sizeof(count));
    ofs.write(reinterpret_cast<const char*>(points.data()), points.size() * sizeof(Point));
}

int main(int argc, char** argv) {
    float x_min = 0.0f, x_max = 100.0f, y_min = 0.0f, y_max = 100.0f;
    float lam = 1.0f;
    float min_dist = -1.0f; // auto
    int k = 30;
    std::string filename = "points.bin";

    auto points = generate_poisson_disk_points_fast(
        x_max - x_min, y_max - y_min,
        min_dist > 0 ? min_dist : std::sqrt((x_max - x_min)*(y_max - y_min)/ (lam * (x_max - x_min)*(y_max - y_min))) * 0.5f,
        lam,
        k
    );

    // Shift points
    for (auto& p : points) {
        p.x += x_min;
        p.y += y_min;
    }

    save_points_binary(points, filename);

    std::cout << "Generated " << points.size() << " Poisson-disk points and saved to " << filename << std::endl;
    return 0;
}
