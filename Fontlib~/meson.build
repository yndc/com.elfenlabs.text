project('fontlib', 'cpp',
  version : '1.0.0',
  default_options : ['cpp_std=c++17', 'default_library=static'])

cmake = import('cmake')

# Get the FreeType subproject and its dependency
freetype_subproj = subproject('freetype2')
freetype_dep = freetype_subproj.get_variable('freetype_dep')

# Include HarfBuzz subproject
harfbuzz_subproj = subproject('harfbuzz')
harfbuzz_lib = harfbuzz_subproj.get_variable('libharfbuzz')

# Include msdf-atlas-gen as a CMake subproject
msdf_atlas_gen_subproj = cmake.subproject('msdf-atlas-gen',
  cmake_options : [
    '-DMSDFGEN_CORE_ONLY=OFF',
    '-DMSDFGEN_BUILD_STANDALONE=OFF',
    '-DMSDFGEN_USE_VCPKG=OFF',
    '-DMSDFGEN_USE_OPENMP=OFF',
    '-DMSDFGEN_USE_CPP11=ON',
    '-DMSDFGEN_USE_SKIA=OFF',
    '-DMSDFGEN_INSTALL=ON',
    '-DMSDFGEN_DYNAMIC_RUNTIME=OFF',
    '-DMSDF_ATLAS_BUILD_STANDALONE=OFF',
    '-DMSDF_ATLAS_USE_VCPKG=OFF',
    '-DMSDF_ATLAS_USE_SKIA=OFF',
    '-DMSDF_ATLAS_NO_ARTERY_FONT=YES',
    '-DMSDF_ATLAS_MSDFGEN_EXTERNAL=OFF',
    '-DMSDF_ATLAS_INSTALL=ON',
    '-DMSDF_ATLAS_DYNAMIC_RUNTIME=OFF',
    '-DBUILD_SHARED_LIBS=OFF',
    '-DCMAKE_BUILD_TYPE=Release'
  ]
)
msdf_atlas_gen_lib = msdf_atlas_gen_subproj.target('msdf-atlas-gen')

# Define include directories
inc_dirs = include_directories(
  'include',
  'src',
  'subprojects/freetype2/include',
  'subprojects/harfbuzz/src',
  'subprojects/msdf-atlas-gen/msdf-atlas-gen',
  'subprojects/msdf-atlas-gen/msdfgen',
  'subprojects/msdf-atlas-gen/msdfgen/core',
  'subprojects/msdf-atlas-gen/__CMake_build'
) 

# Source files
src_files = [
    'src/context.cpp',
    'src/main.cpp',
    'src/shape.cpp',
    'src/texture.cpp',
    'src/atlas.cpp'
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