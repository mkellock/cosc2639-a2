import { HStack, Image, Text, Link, Button } from '@chakra-ui/react';

export interface MusicDetails {
    title: string;
    year: number;
    artist: string;
    imgURL: string;
    webURL: string;
}

export interface InputProps {
    music: MusicDetails;
    subscribe: boolean;
    onInteract(): void;
}

export function Music(props: InputProps) {
    return (
        <HStack margin="5px" padding="2px" width="275px" align="flex-start">
            <Image borderRadius={15} fit="cover" width="100px" height="100px" src={'https://cosc2639-2-s3812552.s3.ap-southeast-2.amazonaws.com/images/' + props.music.imgURL}></Image>
            <table>
                <tr>
                    <td width="60px" valign="top">
                        <Text fontWeight="bold">Title</Text>
                    </td>
                    <td>
                        <Link isExternal href={props.music.webURL} width="200px">
                            {props.music.title}
                        </Link>
                    </td>
                </tr>
                <tr>
                    <td valign="top">
                        <Text fontWeight="bold">Artist</Text>
                    </td>
                    <td>
                        <Text>{props.music.artist}</Text>
                    </td>
                </tr>
                <tr>
                    <td valign="top">
                        <Text fontWeight="bold">Year</Text>
                    </td>
                    <td>
                        <Text>{props.music.year}</Text>
                    </td>
                </tr>
                <tr>
                    <td colSpan={2}>
                        <Button borderRadius={15} onClick={props.onInteract}>
                            {props.subscribe ? 'Subscribe' : 'Unsubscribe'}
                        </Button>
                    </td>
                </tr>
            </table>
        </HStack>
    );
}

export default Music;
