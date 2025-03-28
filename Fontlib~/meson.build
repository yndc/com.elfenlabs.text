project('fontlib', 'cpp',
  version: '1.0.0',
  default_options: ['cpp_std=c++17', 'default_library=static']
)

cmake = import('cmake')

# FreeType subproject
freetype_subproject = subproject('freetype2')
freetype_dep = freetype_subproject.get_variable('freetype_dep')

# HarfBuzz subproject
harfbuzz_subproject = subproject('harfbuzz')
harfbuzz_lib = harfbuzz_subproject.get_variable('libharfbuzz')
harfbuzz_dep = declare_dependency(
  link_with: harfbuzz_lib,
  include_directories: include_directories('subprojects/harfbuzz/src')
)

# msdfgen CMake subproject
msdfgen_subproject = cmake.subproject('msdfgen',
    cmake_options: [
        '-DMSDFGEN_CORE_ONLY=OFF',
        '-DMSDFGEN_BUILD_STANDALONE=OFF',
        '-DMSDFGEN_USE_VCPKG=OFF',
        '-DMSDFGEN_USE_OPENMP=OFF',
        '-DMSDFGEN_USE_CPP11=ON',
        '-DMSDFGEN_USE_SKIA=OFF',
        '-DMSDFGEN_DISABLE_SVG=ON',
        '-DMSDFGEN_INSTALL=ON',
        '-DMSDFGEN_DYNAMIC_RUNTIME=ON',
        '-DCMAKE_BUILD_TYPE=Release',
        '-DBUILD_SHARED_LIBS=OFF'
    ]
)
msdfgen_core_lib = msdfgen_subproject.target('msdfgen-core')
msdfgen_core_dep = declare_dependency(
  link_with: msdfgen_core_lib,
  include_directories: include_directories([
    'subprojects/msdfgen',
    'subprojects/msdfgen/core',
    'subprojects/msdfgen/ext',
    'subprojects/msdfgen/__CMake_build'
  ])
)

msdfgen_ext_lib = msdfgen_subproject.target('msdfgen-ext')
msdfgen_ext_dep = declare_dependency(
  link_with: msdfgen_ext_lib,
  include_directories: include_directories([
    'subprojects/msdfgen',
    'subprojects/msdfgen/core',
    'subprojects/msdfgen/ext',
    'subprojects/msdfgen/__CMake_build'
  ])
)

msdfgen_config_src = meson.current_build_dir() / 'subprojects' / 'msdfgen' / '__CMake_build' / 'msdfgen-config.h'
msdfgen_config_dst_dir = meson.current_build_dir() / 'subprojects' / 'msdfgen' / 'msdfgen'
run_command('python', '-c', 
            'import os, shutil; os.makedirs("' + msdfgen_config_dst_dir + '", exist_ok=True); ' +
            'shutil.copy("' + msdfgen_config_src + '", "' + msdfgen_config_dst_dir / 'msdfgen-config.h' + '")',
            check: true)

# Clipper
clipper_subproject = cmake.subproject('clipper2-cpp',
    cmake_options: [
        '-DCLIPPER2_UTILS=OFF',
        '-DCLIPPER2_EXAMPLES=OFF',
        '-DCLIPPER2_TESTS=OFF',
        '-DUSE_EXTERNAL_GTEST=OFF',
        '-DUSE_EXTERNAL_GBENCHMARK=OFF',
        '-DBUILD_SHARED_LIBS=OFF'
    ]
)
clipper_lib = clipper_subproject.target('Clipper2')
clipper_dep = declare_dependency(
  link_with: clipper_lib,
  include_directories: include_directories('subprojects/clipper2-cpp/Clipper2Lib/include')
)

# Define include directories
inc_dirs = include_directories(
  'include',
  'src',
  'subprojects/msdfgen'
) 

# Source files
src_files = [
    'src/main.cpp'
]

# Build fontlib
fontlib = shared_library('fontlib',
    src_files,
    include_directories : inc_dirs,
    dependencies: [freetype_dep, harfbuzz_dep, msdfgen_core_dep, msdfgen_ext_dep, clipper_dep],
    c_args : ['-fvisibility=hidden'],
    cpp_args : ['-fvisibility=hidden'],
    link_args : ['-fvisibility=hidden'],
    install : true,
    install_dir : meson.project_source_root() / '../Plugins/x64'
)

# Install the header file
install_headers('include/api.h', install_dir : '../Plugins/x64/include')