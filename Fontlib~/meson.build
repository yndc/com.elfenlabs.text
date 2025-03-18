project('fontlib', 'cpp',
  version : '1.0.0',
  default_options : ['cpp_std=c++17', 'default_library=static'])

# Get the FreeType subproject and its dependency
freetype_subproj = subproject('freetype2')
freetype_dep = freetype_subproj.get_variable('freetype_dep')

# Include HarfBuzz subproject
harfbuzz_subproj = subproject('harfbuzz')
harfbuzz_lib = harfbuzz_subproj.get_variable('libharfbuzz')

# Define include directories
inc_dirs = include_directories(
  'include',
  'subprojects/freetype/include',
  'subprojects/harfbuzz/src'
)

# Build fontlib as a shared library with static dependencies
fontlib = shared_library('fontlib',
  'src/fontlib.cpp',
  include_directories : inc_dirs,
  dependencies : [freetype_dep],
  link_with : [harfbuzz_lib],
  install : true,
  install_dir : meson.source_root() / '../Plugins/x64'  # Custom install path
)

# Install the header file
install_headers('include/fontlib.h', install_dir : '../Plugins/x64/include')