#!/usr/bin/env python3

import sys
import os
import configparser
import io
import subprocess


SCRIPT_DIRECTORIES = [
    'largeship',
    'lib',
    'main',
    'misc',
    'standalone',
    'utility',
    'weapon',
]

NL = '\r\n' # Since it's currently a Windows-only game...


def scan_modules():
    result = {}
    for dir in SCRIPT_DIRECTORIES:
        files = [x for x in os.listdir(dir) if x.endswith('.cs') and
                 not (x.endswith('-header.cs') or x.endswith('-footer.cs'))]
        result.update({os.path.splitext(x)[0]: dir for x in files})
    return result


def create_chunk(fn, strip=False, strip_comments=False, strip_empty=False):
    if os.path.exists(fn):
        with io.StringIO() as out:
            with open(fn, 'rU') as content:
                for line in content:
                    if strip:
                        line = line.strip()
                        if line or not strip_empty:
                            if not strip_comments or not line.startswith('//'):
                                out.write(line + NL)
                    else:
                        out.write(line.rstrip() + NL)
                return out.getvalue()


def generate_version_header(version, modules):
    s = """// Generated from ZerothAngel's SEScripts version {}""" + NL + \
        """// Modules: {}""" + NL
    return s.format(version, ', '.join(modules))


def build_script(version, available_modules, spec, strip=False):
    config = configparser.ConfigParser()
    config.read(spec)

    modules = [x.strip() for x in config['script']['modules'].split(',')]
    out_path = config['script']['out']

    chunks = [generate_version_header(version, modules)]

    if strip:
        chunks.append('// NB Leading whitespace stripped to save bytes!' + NL)

    # Headers
    for module in modules:
        if module not in available_modules:
            sys.exit("""No such module '{}'""".format(module))
        header_fn = os.path.join(available_modules[module],
                                 module + '-header.cs')
        chunk = create_chunk(header_fn, strip=strip)
        if chunk:
            chunks.append(chunk)

    # Footers (but not really footers)
    footers = list(modules)
    footers.reverse()
    for module in footers:
        footer_fn = os.path.join(available_modules[module],
                                 module + '-footer.cs')
        chunk = create_chunk(footer_fn, strip=strip)
        if chunk:
            chunks.append(chunk)

    # Finally, the main body
    for module in modules:
        body_fn = os.path.join(available_modules[module],
                               module + '.cs')
        chunk = create_chunk(body_fn, strip=strip, strip_comments=True,
                             strip_empty=True)
        if chunk:
            chunks.append(chunk)

    dirs = os.path.dirname(out_path)
    os.makedirs(dirs, exist_ok=True)
    with open(out_path, 'wt') as out:
        out.write(NL.join(chunks))


def main(specs, strip=False):
    # Fetch version
    if os.path.isdir('.hg'):
        with subprocess.Popen('hg identify', shell=True, stdout=subprocess.PIPE,
                              close_fds=True) as p:
            version, err = p.communicate()
            version = version.decode(sys.getdefaultencoding()).strip()
    else:
        version = 'UNKNOWN'

    available_modules = scan_modules()

    for spec in specs:
        build_script(version, available_modules, spec, strip=strip)



if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(description='Build SE script from parts')
    parser.add_argument('-w', action='store_true',
                        help='strip leading whitespace',
                        dest='strip')
    parser.add_argument('specs', nargs='+',
                        help='script spec files')
    args = parser.parse_args();

    main(args.specs, strip=args.strip)
