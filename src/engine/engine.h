#pragma once

// Dock item list. Not implemented in 0.1.0.

namespace quickdock {

enum class Status { Ok = 0, NotImplemented = 1 };

Status load_items();
Status launch_item(int index);

}  // namespace quickdock
