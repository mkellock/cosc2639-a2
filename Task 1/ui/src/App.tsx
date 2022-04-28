import Login, { LoginDetails } from './Components/Login';
import Register, { RegisterDetails } from './Components/Register';
import MainUI, { UserDetails } from './Components/MainUI';
import { useState } from 'react';
import { ApolloClient, InMemoryCache, gql } from '@apollo/client';
import { ChakraProvider, Wrap, WrapItem, Input, Button, NumberInput, NumberInputField, Text, HStack, Divider, VStack } from '@chakra-ui/react';
import Music, { MusicDetails } from './Components/Music';
import { SearchIcon } from '@chakra-ui/icons';

export function App() {
    const [userDetails, setUserDetails] = useState<UserDetails>();
    const [loginHidden, setLoginHidden] = useState(false);
    const [registerHidden, setRegisterHidden] = useState(true);
    const [mainUIHidden, setMainUIHidden] = useState(true);
    const [loginInError, setLoginInError] = useState(false);
    const [logInErrorMessage, setLogInErrorMessage] = useState<string>();
    const [registerError, setRegisterError] = useState(false);
    const [registerErrorMessage, setRegisterInErrorMessage] = useState<string>();
    const [music, setMusic] = useState<MusicDetails[]>();
    const [subscriptions, setSubscriptions] = useState<MusicDetails[]>();
    const [artist, setArtist] = useState<string | null>(null);
    const [title, setTitle] = useState<string | null>(null);
    const [year, setYear] = useState<number | null>(null);

    let gqlUri = 'http://0.0.0.0/graphql';

    const client = new ApolloClient({
        uri: gqlUri,
        cache: new InMemoryCache(),
    });

    const onLoginSubmit = (loginDetails: LoginDetails) => {
        // Call API to authenticate user
        const getLoginDetails = gql`
            query userByEmailPassword($email: String!, $password: String!) {
                userByEmailPassword(email: $email, password: $password) {
                    username
                    eMail
                }
            }
        `;

        const loginVariables = {
            email: loginDetails.email,
            password: loginDetails.password,
        };

        client
            .query({
                query: getLoginDetails,
                variables: loginVariables,
            })
            .then((result) => {
                // If login is unsuccessful
                if (result.data.userByEmailPassword === null) {
                    setLoginInError(true);
                    setLogInErrorMessage('Email or password is invalid');
                } else {
                    // If authentication passes
                    setMainUIHidden(false);
                    setLoginHidden(true);
                    setLoginInError(false);
                    setLogInErrorMessage('');

                    // Load the user's data
                    loadUser({ ...result.data.userByEmailPassword });
                }
            });
    };

    const onLogin = () => {
        setLoginInError(false);
        setLogInErrorMessage('');
        setLoginHidden(false);
        setRegisterHidden(true);
    };

    const onRegister = () => {
        setLoginInError(false);
        setLogInErrorMessage('');
        setLoginHidden(true);
        setRegisterHidden(false);
    };

    const onRegisterSubmit = (registerDetails: RegisterDetails) => {
        // Call API to register user
        const registerUser = gql`
            mutation registerUser($email: String!, $username: String!, $password: String!) {
                registerUser(email: $email, username: $username, password: $password)
            }
        `;

        const registerUserVariables = {
            email: registerDetails.email,
            username: registerDetails.username,
            password: registerDetails.password,
        };

        client
            .mutate({
                mutation: registerUser,
                variables: registerUserVariables,
            })
            .then((result) => {
                if (result.data.registerUser) {
                    // If registration passes
                    setRegisterHidden(true);
                    setRegisterError(false);

                    // Go back to the login screen
                    onLogoutClick();
                } else {
                    // If id exists
                    setRegisterError(true);
                    setRegisterInErrorMessage('The EMail already exists');
                }
            });
    };

    const onLogoutClick = () => {
        // Set up the UI
        setMainUIHidden(true);
        setLoginHidden(false);

        // Unset the user's login
        setUserDetails(undefined);
    };

    const loadUser = (userData: UserDetails) => {
        // Set the login details
        setUserDetails({ ...userData });
        loadMusic(null, null, null);
        loadSubscriptions(userData);
    };

    const searchMusic = () => {
        loadMusic(artist, title, year);
    };

    const loadMusic = (searchArtist: string | null, searchTitle: string | null, searchYear: number | null) => {
        const getMusic = gql`
            query ($artist: String, $title: String, $year: Int) {
                musicByTitleYearArtist(artist: $artist, title: $title, year: $year) {
                    title
                    artist
                    year
                    webURL
                    imgURL
                }
            }
        `;

        const musicVariables = {
            artist: searchArtist,
            title: searchTitle,
            year: searchYear,
        };

        client
            .query({
                query: getMusic,
                variables: musicVariables,
            })
            .then((result) => {
                let loadedMusic: MusicDetails[] = [];

                result.data.musicByTitleYearArtist.forEach((element: any) => {
                    loadedMusic.push({ ...element });
                });

                setMusic(loadedMusic);
            });
    };

    const subscribe = (musicItem: MusicDetails) => {
        const putSubscribe = gql`
            mutation ($artist: String!, $title: String!, $email: String!) {
                registerSubscription(artist: $artist, title: $title, email: $email)
            }
        `;

        const subscribeVariables = {
            artist: musicItem.artist,
            title: musicItem.title,
            email: userDetails?.eMail,
        };

        client
            .mutate({
                mutation: putSubscribe,
                variables: subscribeVariables,
            })
            .then(() => {
                loadSubscriptions(userDetails);
            });
    };

    const unsubscribe = (musicItem: MusicDetails) => {
        const putUnsubscribe = gql`
            mutation ($artist: String!, $title: String!, $email: String!) {
                deleteSubscription(artist: $artist, title: $title, email: $email)
            }
        `;

        const unsubscribeVariables = {
            artist: musicItem.artist,
            title: musicItem.title,
            email: userDetails?.eMail,
        };

        client
            .mutate({
                mutation: putUnsubscribe,
                variables: unsubscribeVariables,
            })
            .then(() => {
                loadSubscriptions(userDetails);
            });
    }

    const loadSubscriptions = (userDetailsInput: UserDetails | undefined) => {
        const getSubscriptions = gql`
            query ($email: String!) {
                subscriptionByEmail(email: $email) {
                    title
                    artist
                    year
                    webURL
                    imgURL
                }
            }
        `;

        const getSubscriptionsVariables = {
            email: userDetailsInput?.eMail,
        };

        client
            .query({
                query: getSubscriptions,
                variables: getSubscriptionsVariables,
            })
            .then((result) => {
                let loadedSubscriptions: MusicDetails[] = [];

                result.data.subscriptionByEmail.forEach((element: any) => {
                    loadedSubscriptions.push({ ...element });
                });

                setSubscriptions(loadedSubscriptions);
            });
    };

    return (
        <ChakraProvider>
            <Login onSubmit={onLoginSubmit} onRegister={onRegister} hidden={loginHidden} inError={loginInError} errorMessage={logInErrorMessage} />
            <Register hidden={registerHidden} onSubmit={onRegisterSubmit} inError={registerError} errorMessage={registerErrorMessage} onLogin={onLogin} />
            <MainUI hidden={mainUIHidden} onLogoutClick={onLogoutClick} userDetails={userDetails}>
                <HStack spacing={3} align="top">
                    <VStack>
                        <Text>Subscriptions</Text>
                        <Wrap width="300px">
                            {subscriptions?.map((musicItem: MusicDetails) => (
                                <WrapItem>
                                    <Music key={musicItem.title} music={musicItem} onInteract={() => unsubscribe(musicItem)} subscribe={false} />
                                </WrapItem>
                            ))}
                        </Wrap>
                    </VStack>
                    <Divider orientation="vertical" height="300px" />
                    <VStack>
                        <table>
                            <tr>
                                <td>
                                    <Input borderRadius={15} placeholder="Artist" onChange={(e) => setArtist(e.target.value.length > 0 ? e.target.value : null)}></Input>
                                </td>
                                <td>
                                    <Input borderRadius={15} placeholder="Title" onChange={(e) => setTitle(e.target.value.length > 0 ? e.target.value : null)}></Input>
                                </td>
                                <td>
                                    <NumberInput>
                                        <NumberInputField borderRadius={15} placeholder="Year" onChange={(e) => setYear(e.target.value.length > 0 ? parseInt(e.target.value) : null)} />
                                    </NumberInput>
                                </td>
                                <td colSpan={3} align="right">
                                    <Button borderRadius={15} leftIcon={<SearchIcon />} onClick={() => searchMusic()}>
                                        Search
                                    </Button>
                                </td>
                            </tr>
                        </table>
                        <Wrap>
                            {music !== undefined && music?.length > 0 ? (
                                music?.map((musicItem: MusicDetails) => (
                                    <WrapItem>
                                        <Music key={musicItem.title} music={musicItem} onInteract={() => subscribe(musicItem)} subscribe={true} />
                                    </WrapItem>
                                ))
                            ) : (
                                <WrapItem>
                                    <Text>No result is retrieved. Please query again</Text>
                                </WrapItem>
                            )}
                        </Wrap>
                    </VStack>
                </HStack>
            </MainUI>
        </ChakraProvider>
    );
}

export default App;
