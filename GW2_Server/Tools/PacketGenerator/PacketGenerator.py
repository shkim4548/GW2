# -*- coding: utf-8 -*-

import argparse
import jinja2
import ProtoParser

def main():
    arg_parser = argparse.ArgumentParser(description='PacketGenerator')
    arg_parser.add_argument('--path', type=str,
                            default='../../Common/Protobuf/bin/Protocol.proto',
                            help='proto path')
    arg_parser.add_argument('--output', type=str, default='ClientPacketHandler', help='output file prefix')
    arg_parser.add_argument('--recv', type=str, default='C_', help='recv convention')
    arg_parser.add_argument('--send', type=str, default='S_', help='send convention')
    args = arg_parser.parse_args()

    parser = ProtoParser.ProtoParser(1000, args.recv, args.send)
    parser.parse_proto(args.path)

    file_loader = jinja2.FileSystemLoader('Templates', encoding='utf-8')
    env = jinja2.Environment(loader=file_loader)

    template1 = env.get_template('PacketHandler.h')
    output1 = template1.render(parser=parser, output=args.output)
    header_file = args.output + '.h'
    with open(header_file, 'w', encoding='utf-8') as f:
        f.write(output1)
    print(f'Generated: {header_file}')

    template2 = env.get_template('PacketManager.cs')
    output2 = template2.render(parser=parser, output=args.output)
    cs_file = 'ClientPacketHandler.cs'
    try:
        with open(cs_file, 'w', encoding='utf-8') as f:
            f.write(output2)
        print(f'Generated: {cs_file}')
    except Exception as e:
        print("filewrite failed:", e)

    # (옵션) 콘솔 출력
    print(output1)
    print(output2)

if __name__ == '__main__':
	main()