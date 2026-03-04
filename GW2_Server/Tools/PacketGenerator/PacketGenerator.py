# -*- coding: utf-8 -*-

import argparse
import jinja2
import ProtoParser
import os
import sys


def main():
    work_dir = os.getcwd()

    arg_parser = argparse.ArgumentParser(description='PacketGenerator')
    arg_parser.add_argument(
        '--path',
        type=str,
        default='Protocol.proto',
        help='proto path (relative to execution directory)'
    )
    arg_parser.add_argument('--output', type=str, default='ClientPacketHandler')
    arg_parser.add_argument('--recv', type=str, default='C_')
    arg_parser.add_argument('--send', type=str, default='S_')
    args = arg_parser.parse_args()

    proto_path = args.path
    if not os.path.isabs(proto_path):
        proto_path = os.path.normpath(os.path.join(work_dir, proto_path))

    print('[INFO] WorkDir  :', work_dir)
    print('[INFO] ProtoPath:', proto_path)

    if not os.path.exists(proto_path):
        print('[ERROR] Proto file not found!')
        return

    parser = ProtoParser.ProtoParser(1000, args.recv, args.send)
    parser.parse_proto(proto_path)

    if getattr(sys, 'frozen', False):
        template_dir = os.path.join(os.path.dirname(sys.executable), 'Templates')
    else:
        template_dir = os.path.join(os.path.dirname(__file__), 'Templates')

    print('[INFO] TemplateDir:', template_dir)

    file_loader = jinja2.FileSystemLoader(template_dir, encoding='utf-8')
    env = jinja2.Environment(loader=file_loader)

    template_h = env.get_template('PacketHandler.h')
    output_h = template_h.render(parser=parser, output=args.output)

    header_file = os.path.join(work_dir, args.output + '.h')
    with open(header_file, 'w', encoding='utf-8') as f:
        f.write(output_h)

    print('[OK] Generated:', header_file)

    template_cs = env.get_template('PacketManager.cs')
    output_cs = template_cs.render(parser=parser, output=args.output)

    cs_file = os.path.join(work_dir, 'ServerPacketHandler.cs')
    with open(cs_file, 'w', encoding='utf-8') as f:
        f.write(output_cs)

    print('[OK] Generated:', cs_file)
    print('[DONE] Packet generation completed')


if __name__ == '__main__':
    main()
