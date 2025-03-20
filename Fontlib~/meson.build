project('fontlib', 'cpp',
  version : '1.0.0',
  default_options : ['cpp_std=c++17', 'default_library=static'])

# Get the FreeType subproject and its dependency
freetype_subproj = subproject('freetype2')
freetype_dep = freetype_subproj.get_variable('freetype_dep')

# Include HarfBuzz subproject
harfbuzz_subproj = subproject('harfbuzz')
harfbuzz_lib = harfbuzz_subproj.get_variable('libharfbuzz')

# Include msdf-atlas-gen as a CMake subproject
msdf_atlas_gen_subproj = subproject('msdf-atlas-gen')
msdf_atlas_gen_lib = msdf_atlas_gen_subproj.target('msdf-atlas-gen')

# Define include directories
inc_dirs = include_directories(
  'include',
  'src',
  'subprojects/freetype/include',
  'subprojects/harfbuzz/src',
  'subprojects/msdf-atlas-gen'
)

# Source files
src_files = [
    'src/context.cpp',
    'src/main.cpp',
    'src/shape.cpp',
]

# Build fontlib as a shared library with static dependencies
fontlib = shared_library('fontlib',
  src_files,
  include_directories : inc_dirs,
  dependencies : [freetype_dep],
  link_with : [harfbuzz_lib, msdf_atlas_gen_lib],
  cpp_args : ['-fvisibility=hidden'],
  link_args : ['-fvisibility=hidden'],
  install : true,
  install_dir : meson.project_source_root() / '../Plugins/x64'
)

# Install the header file
install_headers('include/api.h', install_dir : '../Plugins/x64/include')